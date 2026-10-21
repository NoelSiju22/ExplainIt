
from pathlib import Path
import joblib

BASE_DIR = Path(__file__).resolve().parent
MODEL_PATH = BASE_DIR / "model.joblib"

if not MODEL_PATH.exists():
    raise FileNotFoundError("Run python train.py first.")

model = joblib.load(MODEL_PATH)

print("ExplainIt Offline Document Classifier")
print("Paste your document below.")
print("When finished, type END on a new line.")
print("Type EXIT on the first line to quit.\n")

while True:
    lines = []

    while True:
        try:
            line = input()
        except EOFError:
            break

        if not lines and line.strip().upper() == "EXIT":
            print("Goodbye!")
            raise SystemExit

        if line.strip().upper() == "END":
            break

        lines.append(line)

    document = "\n".join(lines).strip()

    if not document:
        print("No text entered. Please try again.\n")
        continue

    predicted_category = model.predict([document])[0]
    probabilities = model.predict_proba([document])[0]
    confidence = max(probabilities) * 100

    print("\n--- ExplainIt Result ---")
    print(f"Predicted category: {predicted_category}")
    print(f"Model score: {confidence:.1f}%")
    print("------------------------\n")

    if not lines:
        break
