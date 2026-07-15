// StoreKitShim: a flat C ABI over StoreKit 2 for the StoryCAD desktop (Skia) head.
// C# cannot call StoreKit 2 directly (Swift-only, async), so every export here is a
// synchronous C function that copies its arguments, runs the async work in a Task,
// and delivers a UTF-8 JSON payload to the caller's callback.
// The JSON contracts are documented in the shim contract
// (StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md); keep both in sync.

import Foundation
import StoreKit

public typealias CompletionCallback = @convention(c) (Int64, UnsafePointer<CChar>) -> Void

// MARK: - Shared state

private var transactionCallback: CompletionCallback?
private var updatesTask: Task<Void, Never>?
private let stateLock = NSLock()

private let isoFormatter: ISO8601DateFormatter = {
    let f = ISO8601DateFormatter()
    f.formatOptions = [.withInternetDateTime]
    return f
}()

// MARK: - JSON helpers

private func jsonString(_ obj: [String: Any]) -> String {
    guard JSONSerialization.isValidJSONObject(obj),
          let data = try? JSONSerialization.data(withJSONObject: obj),
          let s = String(data: data, encoding: .utf8) else {
        return #"{"ok":false,"error":"failed to encode response JSON","code":"json-encode"}"#
    }
    return s
}

private func failureJson(_ message: String, code: String) -> String {
    jsonString(["ok": false, "error": message, "code": code])
}

// The pointer handed to the callback is only valid for the duration of the call;
// the C# side copies it immediately.
private func deliver(_ cb: CompletionCallback, _ requestId: Int64, _ json: String) {
    json.withCString { cb(requestId, $0) }
}

// Synchronous accessor so async contexts never touch the NSLock directly.
private func currentTransactionCallback() -> CompletionCallback? {
    stateLock.lock()
    defer { stateLock.unlock() }
    return transactionCallback
}

// MARK: - Mapping helpers

private func isoPeriod(_ p: Product.SubscriptionPeriod) -> String {
    switch p.unit {
    case .day: return "P\(p.value)D"
    case .week: return "P\(p.value)W"
    case .month: return "P\(p.value)M"
    case .year: return "P\(p.value)Y"
    @unknown default: return "P\(p.value)D"
    }
}

private func renewalStateString(_ state: Product.SubscriptionInfo.RenewalState) -> String {
    switch state {
    case .subscribed: return "active"
    case .inGracePeriod: return "gracePeriod"
    case .inBillingRetryPeriod: return "billingRetry"
    case .expired: return "expired"
    case .revoked: return "revoked"
    // Fail closed, matching the C# MapState default: an unknown state must never grant access.
    default: return "expired"
    }
}

// Best-effort renewal detail for an auto-renewable product. Returns nil when the
// store has no status to offer (e.g. offline, or a non-subscription product).
private func subscriptionMeta(productId: String, originalTransactionId: UInt64) async -> (state: String, willAutoRenew: Bool)? {
    guard let product = try? await Product.products(for: [productId]).first,
          let sub = product.subscription,
          let statuses = try? await sub.status, !statuses.isEmpty else { return nil }
    let status = statuses.first { s in
        if case .verified(let t) = s.transaction { return t.originalID == originalTransactionId }
        return false
    } ?? statuses[0]
    var willAutoRenew = false
    if case .verified(let info) = status.renewalInfo { willAutoRenew = info.willAutoRenew }
    return (renewalStateString(status.state), willAutoRenew)
}

// Fallback state when no RenewalInfo is reachable. A transaction that
// Transaction.currentEntitlements still yields despite a past expirationDate is
// in grace period (Apple drops it from the sequence once entitlement truly ends).
private func computedState(_ t: Transaction) -> String {
    if t.revocationDate != nil { return "revoked" }
    if let exp = t.expirationDate, exp < Date() { return "gracePeriod" }
    return "active"
}

private func entitlementDict(_ t: Transaction, jws: String) async -> [String: Any] {
    var state = computedState(t)
    var willAutoRenew = false
    if t.productType == .autoRenewable,
       let meta = await subscriptionMeta(productId: t.productID, originalTransactionId: t.originalID) {
        state = meta.state
        willAutoRenew = meta.willAutoRenew
    }
    var dict: [String: Any] = [
        "productId": t.productID,
        "transactionId": String(t.id),
        "originalTransactionId": String(t.originalID),
        "purchaseDate": isoFormatter.string(from: t.purchaseDate),
        "state": state,
        "willAutoRenew": willAutoRenew,
        "jws": jws,
    ]
    if let exp = t.expirationDate {
        dict["expirationDate"] = isoFormatter.string(from: exp)
    }
    return dict
}

private func entitlementsJson() async -> String {
    var items: [[String: Any]] = []
    for await result in Transaction.currentEntitlements {
        // Unverified transactions are rejected, never surfaced as entitlements.
        guard case .verified(let t) = result else { continue }
        items.append(await entitlementDict(t, jws: result.jwsRepresentation))
    }
    return jsonString(["ok": true, "entitlements": items])
}

// MARK: - Exports

@_cdecl("storycad_iap_set_transaction_callback")
public func storycad_iap_set_transaction_callback(_ cb: @escaping CompletionCallback) {
    stateLock.lock()
    transactionCallback = cb
    let alreadyListening = updatesTask != nil
    if !alreadyListening {
        // Started exactly once. Unfinished/renewed transactions replay here on
        // every launch until finished, so each one is delivered then finished.
        updatesTask = Task {
            for await update in Transaction.updates {
                guard case .verified(let t) = update else { continue }
                let dict = await entitlementDict(t, jws: update.jwsRepresentation)
                let json = jsonString(["ok": true, "entitlements": [dict]])
                if let callback = currentTransactionCallback() { deliver(callback, -1, json) }
                await t.finish()
            }
        }
    }
    stateLock.unlock()
}

@_cdecl("storycad_iap_get_products")
public func storycad_iap_get_products(_ requestId: Int64, _ productIdsJson: UnsafePointer<CChar>, _ cb: @escaping CompletionCallback) {
    let idsJson = String(cString: productIdsJson)
    Task {
        guard let data = idsJson.data(using: .utf8),
              let ids = (try? JSONSerialization.jsonObject(with: data)) as? [String] else {
            deliver(cb, requestId, failureJson("productIdsJson is not a JSON array of strings", code: "bad-request"))
            return
        }
        do {
            let products = try await Product.products(for: ids)
            let items: [[String: Any]] = products.map { p in
                var dict: [String: Any] = [
                    "id": p.id,
                    "displayName": p.displayName,
                    "description": p.description,
                    "displayPrice": p.displayPrice,
                    "rawPrice": "\(p.price)",
                    "currency": p.priceFormatStyle.currencyCode,
                    "hasIntroOffer": p.subscription?.introductoryOffer != nil,
                ]
                if let period = p.subscription?.subscriptionPeriod {
                    dict["subscriptionPeriod"] = isoPeriod(period)
                }
                return dict
            }
            deliver(cb, requestId, jsonString(["ok": true, "products": items]))
        } catch {
            deliver(cb, requestId, failureJson(error.localizedDescription, code: String(describing: error)))
        }
    }
}

@_cdecl("storycad_iap_purchase")
public func storycad_iap_purchase(_ requestId: Int64, _ productId: UnsafePointer<CChar>, _ appAccountToken: UnsafePointer<CChar>?, _ cb: @escaping CompletionCallback) {
    let id = String(cString: productId)
    let token = appAccountToken.map { String(cString: $0) }
    Task {
        do {
            guard let product = try await Product.products(for: [id]).first else {
                deliver(cb, requestId, failureJson("product \(id) not found in the store", code: "unknown-product"))
                return
            }
            var options: Set<Product.PurchaseOption> = []
            if let token {
                guard let uuid = UUID(uuidString: token) else {
                    deliver(cb, requestId, failureJson("appAccountToken is not a valid UUID", code: "bad-request"))
                    return
                }
                options.insert(.appAccountToken(uuid))
            }
            let result = try await product.purchase(options: options)
            switch result {
            case .success(let verification):
                switch verification {
                case .verified(let t):
                    deliver(cb, requestId, jsonString([
                        "ok": true,
                        "status": "success",
                        "transactionId": String(t.id),
                        "originalTransactionId": String(t.originalID),
                        "productId": t.productID,
                        "jws": verification.jwsRepresentation,
                    ]))
                    await t.finish()
                case .unverified(_, let verificationError):
                    deliver(cb, requestId, failureJson("transaction failed StoreKit verification", code: String(describing: verificationError)))
                }
            case .userCancelled:
                deliver(cb, requestId, jsonString(["ok": true, "status": "userCancelled"]))
            case .pending:
                deliver(cb, requestId, jsonString(["ok": true, "status": "pending"]))
            @unknown default:
                deliver(cb, requestId, failureJson("unknown purchase result", code: "unknown-result"))
            }
        } catch {
            deliver(cb, requestId, failureJson(error.localizedDescription, code: String(describing: error)))
        }
    }
}

@_cdecl("storycad_iap_current_entitlements")
public func storycad_iap_current_entitlements(_ requestId: Int64, _ cb: @escaping CompletionCallback) {
    Task {
        deliver(cb, requestId, await entitlementsJson())
    }
}

@_cdecl("storycad_iap_restore")
public func storycad_iap_restore(_ requestId: Int64, _ cb: @escaping CompletionCallback) {
    Task {
        do {
            // Shows a system sign-in prompt; callers must only invoke this from
            // an explicit Restore Purchases action.
            try await AppStore.sync()
            deliver(cb, requestId, await entitlementsJson())
        } catch {
            deliver(cb, requestId, failureJson(error.localizedDescription, code: String(describing: error)))
        }
    }
}

@_cdecl("storycad_iap_subscription_status")
public func storycad_iap_subscription_status(_ requestId: Int64, _ productId: UnsafePointer<CChar>, _ cb: @escaping CompletionCallback) {
    let id = String(cString: productId)
    Task {
        do {
            guard let product = try await Product.products(for: [id]).first else {
                deliver(cb, requestId, failureJson("product \(id) not found in the store", code: "unknown-product"))
                return
            }
            guard let sub = product.subscription else {
                deliver(cb, requestId, failureJson("product \(id) is not a subscription", code: "not-a-subscription"))
                return
            }
            let statuses = try await sub.status
            guard !statuses.isEmpty else {
                deliver(cb, requestId, failureJson("no subscription status available for \(id)", code: "no-status"))
                return
            }
            guard case .verified(let t) = statuses[0].transaction else {
                deliver(cb, requestId, failureJson("subscription status transaction failed StoreKit verification", code: "unverified"))
                return
            }
            var dict = await entitlementDict(t, jws: statuses[0].transaction.jwsRepresentation)
            dict["state"] = renewalStateString(statuses[0].state)
            if case .verified(let info) = statuses[0].renewalInfo {
                dict["willAutoRenew"] = info.willAutoRenew
            }
            deliver(cb, requestId, jsonString(["ok": true, "status": dict]))
        } catch {
            deliver(cb, requestId, failureJson(error.localizedDescription, code: String(describing: error)))
        }
    }
}
