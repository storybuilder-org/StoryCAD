import sys
from pathlib import Path
from pythonnet import load
load("coreclr")

import clr

publish_dir = Path("/Users/jake/Repos/StoryCAD/publish").resolve()
sys.path.insert(0, str(publish_dir))
clr.AddReference("StoryCADLib")

from StoryCADLib.Services.API import StoryCADApi
from StoryCADLib.Services.IoC import BootStrapper
from StoryCADLib.Models import StoryItemType
from CommunityToolkit.Mvvm.DependencyInjection import Ioc
from System import Guid as NetGuid, String, Object
from System.Collections.Generic import Dictionary


def unwrap(op, label=""):
    """OperationResult<T> → Payload, or raise."""
    if not op.IsSuccess:
        raise RuntimeError(f"{label}: {op.ErrorMessage}")
    return op.Payload


def sync(task):
    """Task<T> → T."""
    return task.GetAwaiter().GetResult()


def props(**kwargs):
    """Python dict → Dictionary[String, Object]."""
    d = Dictionary[String, Object]()
    for k, v in kwargs.items():
        d[k] = v
    return d


def to_net_guid(g):
    """Whatever Python is holding → System.Guid."""
    return NetGuid.Parse(g.ToString()) if hasattr(g, "ToString") else NetGuid.Parse(str(g))


# ─── boot ───────────────────────────────────────────────────────────────────
BootStrapper.Initialise(False)
api = Ioc.Default.GetRequiredService[StoryCADApi]()

# ─── 1. create the outline ──────────────────────────────────────────────────
print("Creating outline...")
guids = unwrap(sync(api.CreateEmptyOutline(
    "The Lighthouse at the Edge of Reason",
    "Jake (via Python)",
    "0"
)), "CreateEmptyOutline")
print(f"  → {len(guids)} root elements created")

all_elements = unwrap(api.GetAllElements(), "GetAllElements")
overview = next(e for e in all_elements if e.ElementType == StoryItemType.StoryOverview)
overview_guid = str(overview.Uuid)
print(f"  → Overview: {overview.Name} ({overview_guid})")

# ─── 2. add a StoryWorld ────────────────────────────────────────────────────
print("\nBuilding the world...")
world_guid = unwrap(api.AddElement(
    StoryItemType.StoryWorld, overview_guid, "Coastal Britain, 1887"
), "AddElement(World)")
print(f"  → World: {world_guid}")

# ─── 3. settings ────────────────────────────────────────────────────────────
print("\nAdding settings...")
lighthouse_guid = unwrap(api.AddElement(
    StoryItemType.Setting, overview_guid, "Tregarrow Lighthouse"
), "AddElement(Lighthouse)")
village_guid = unwrap(api.AddElement(
    StoryItemType.Setting, overview_guid, "Porthlevy Village"
), "AddElement(Village)")
print(f"  → 2 settings")

# ─── 4. characters (add + update properties separately) ────────────────────
print("\nAdding characters...")

def add_character(name, **properties):
    g = unwrap(api.AddElement(StoryItemType.Character, overview_guid, name), f"AddElement({name})")
    if properties:
        unwrap(api.UpdateElementProperties(to_net_guid(g), props(**properties)),
               f"UpdateElementProperties({name})")
    return g

keeper_guid = add_character("Elias Trevethan", Role="Protagonist", Age="52", Sex="Male")
apprentice_guid = add_character("Wren Carlyon", Role="Deuteragonist", Age="19", Sex="Female")
thing_guid = add_character("The Thing in the Lamp", Role="Antagonist", Archetype="The Shadow")
print(f"  → 3 characters")

# ─── 5. relationship ────────────────────────────────────────────────────────
print("\nAdding relationship...")
unwrap(api.AddRelationship(
    to_net_guid(keeper_guid),
    to_net_guid(apprentice_guid),
    "Mentor and reluctant apprentice; Elias resents her cheerfulness.",
    True
), "AddRelationship")
print(f"  → Elias ↔ Wren")

# ─── 6. central problem + beat sheet ────────────────────────────────────────
print("\nAdding central problem...")
problem_guid = unwrap(api.AddElement(
    StoryItemType.Problem, overview_guid, "The Light Must Not Go Out"
), "AddElement(Problem)")

print("\nAvailable beat sheets:")
beat_sheets = list(unwrap(api.GetBeatSheetNames(), "GetBeatSheetNames"))
for bs in beat_sheets[:5]:
    print(f"  - {bs}")
chosen = beat_sheets[0]
print(f"\nApplying beat sheet: {chosen}")
unwrap(api.ApplyBeatSheetToProblem(to_net_guid(problem_guid), chosen), "ApplyBeatSheetToProblem")

struct_payload = unwrap(api.GetProblemStructure(to_net_guid(problem_guid)), "GetProblemStructure")
print(f"  → Structure: '{struct_payload.Item1}'")
beat_list = list(struct_payload.Item3)
print(f"  → {len(beat_list)} beats")

# ─── 7. scenes + bind to beats ──────────────────────────────────────────────
print("\nAdding scenes and binding to beats...")
scene_titles = [
    "The Lamp Flickers",
    "Wren Arrives by Boat",
    "Something in the Glass",
    "Elias Refuses to Speak of It",
    "The Light Goes Out",
]
scene_guids = []
for title in scene_titles:
    g = unwrap(api.AddElement(StoryItemType.Scene, overview_guid, title), f"AddElement({title})")
    scene_guids.append(g)

problem_net = to_net_guid(problem_guid)
bind_count = min(len(scene_guids), len(beat_list))
for i in range(bind_count):
    unwrap(api.AssignElementToBeat(
        problem_net, i, to_net_guid(scene_guids[i])
    ), f"AssignElementToBeat({i})")
print(f"  → {len(scene_guids)} scenes, first {bind_count} assigned to beats")

# ─── 8. cast characters into scenes ─────────────────────────────────────────
print("\nCasting characters into opening scenes...")
unwrap(api.AddCastMember(to_net_guid(scene_guids[0]), to_net_guid(keeper_guid)),
       "AddCastMember(Elias → Scene 1)")
unwrap(api.AddCastMember(to_net_guid(scene_guids[1]), to_net_guid(apprentice_guid)),
       "AddCastMember(Wren → Scene 2)")
print(f"  → 2 cast assignments")

# ─── 9. search test ─────────────────────────────────────────────────────────
print("\nSearching for 'lamp'...")
hits = list(unwrap(api.SearchForText("lamp"), "SearchForText"))
for h in hits:
    print(f"  - {h['Type']}: {h['Name']}")

# ─── 10. save ───────────────────────────────────────────────────────────────
out_path = "/tmp/lighthouse.stbx"
print(f"\nWriting outline to {out_path}...")
unwrap(sync(api.WriteOutline(out_path)), "WriteOutline")
print(f"  → wrote {Path(out_path).stat().st_size} bytes")

print("\nDone. Open it in StoryCAD to verify.")
