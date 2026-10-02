namespace StoryCollaborator.Models
{
    /// <summary>
    /// Ambient slot the chat HTTP handler drops the parsed <c>X-Collab-Cost</c> into.
    ///
    /// Semantic Kernel hides <see cref="HttpResponseMessage"/> from callers —
    /// <c>GetChatMessageContentAsync</c> returns content, not a response — so a response
    /// header cannot be read at the call site. The <see cref="ActivationJwtHandler"/>
    /// already sitting in the chat pipeline reads it and hands it back out of band.
    ///
    /// An AsyncLocal slot rather than a field on the handler: the handler is built once
    /// per kernel and outlives any single turn, so a mutable field would mis-attribute a
    /// cost the moment two turns overlapped. The slot reference flows *down* into the
    /// handler with the execution context and the handler mutates the object the caller
    /// still holds, which stays correct per logical call however the calls interleave.
    /// </summary>
    internal static class ChatCostCapture
    {
        /// <summary>Mutable cell shared between the scope owner and the handler.</summary>
        internal sealed class Slot
        {
            internal ProxyCostInfo? Cost;
        }

        private static readonly AsyncLocal<Slot?> Current = new();

        /// <summary>
        /// Opens a capture scope around one chat turn. Read <see cref="Scope.Cost"/> after
        /// the call completes; dispose to close the scope.
        ///
        /// Not re-entrant: <see cref="Scope.Dispose"/> clears the slot rather than restoring
        /// a previous one, so a nested scope would orphan its parent. There is one call site
        /// and no nesting today; nesting would need a save-and-restore here.
        /// </summary>
        internal static Scope Begin()
        {
            var slot = new Slot();
            Current.Value = slot;
            return new Scope(slot);
        }

        /// <summary>
        /// Records a cost against the open scope, if there is one. A no-op otherwise —
        /// nothing structurally prevents the handler firing outside a scope, and a
        /// diagnostic line must never be able to crash a chat turn.
        /// </summary>
        internal static void Record(ProxyCostInfo? cost)
        {
            var slot = Current.Value;
            if (slot != null)
                slot.Cost = cost;
        }

        internal sealed class Scope : IDisposable
        {
            private readonly Slot _slot;

            internal Scope(Slot slot) => _slot = slot;

            /// <summary>Cost captured during this scope, or null if none was reported.</summary>
            internal ProxyCostInfo? Cost => _slot.Cost;

            public void Dispose() => Current.Value = null;
        }
    }
}
