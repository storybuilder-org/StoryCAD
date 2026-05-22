"""Smoke test: one method per shape, then save."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import storycad


def run() -> None:
    sc = storycad.StoryCAD()

    # Shape 4 (async): create
    guids = sc.create_empty_outline("Coverage Test", "Tester", "0")
    print(f"create_empty_outline → {len(guids)} elements")

    # Shape 3 (list of StoryElement → ElementSummary)
    elements = sc.get_all_elements()
    print(f"get_all_elements → {len(elements)} ({elements[0].element_type})")
    overview = next(e for e in elements if e.element_type == "StoryOverview")

    # Shape 2 (returns Guid)
    char = sc.add_element(sc.item_type.Character, overview.uuid, "Test Hero")
    print(f"add_element → {char}")

    # Shape 1 (primitive: int) — remove_references on an unused guid returns 0
    import uuid as _uuid
    count = sc.remove_references(_uuid.uuid4())
    print(f"remove_references → {count}")

    # Shape 3 again (IEnumerable<string>)
    examples = sc.get_examples("Tone")
    print(f"get_examples('Tone') → {len(examples)} items")

    # Shape 5 (ValueTuple → dataclass)
    questions = sc.get_key_questions("Character")
    print(f"get_key_questions('Character') → {len(questions)}; first topic: {questions[0].topic}")

    # Beat sheet path: shape 5 + shape 1 chain
    names = sc.get_beat_sheet_names()
    print(f"get_beat_sheet_names → {len(names)}")
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "Central Problem")
    sc.apply_beat_sheet_to_problem(problem, names[0])
    struct = sc.get_problem_structure(problem)
    print(f"get_problem_structure → '{struct.title}' / {len(struct.beats)} beats")

    # Shape 4: save (async)
    out = "/tmp/coverage_test.stbx"
    sc.write_outline(out)
    print(f"write_outline → {Path(out).stat().st_size} bytes")


if __name__ == "__main__":
    run()
