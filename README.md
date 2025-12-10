Kutumb — Mini Unity Demo
=========================

What this contains
- Simple Login scene with a "Sign in with Apple" button that simulates success/failure.
- Humanoid scene with a character, a "Play Reaction" button and a ReactionController that plays Smile/Sad sequence and lip-syncs a local audio file.

Key scripts (Assets/Scripts)
- `LoginManager.cs` — handles sign-in button and loads `HumanoidScene` on success.
- `ReactionController.cs` — plays Smile→Sad→Smile→Sad, coordinates audio and lip-sync.
- `LipSyncFake.cs` — simple mouth open/close faux lip-sync using blendshapes or an Animator bool.
- `SceneLoader.cs` — helper to load scenes by name.

Setup notes
1. Add two scenes to Build Settings: `LoginScene` (first) and `HumanoidScene`.
2. In `LoginScene`, create a Canvas with a Button named "Sign in with Apple" and a Text for feedback. Attach `LoginManager` and wire the Button and Text.
3. In `HumanoidScene`, place your humanoid model (Mixamo/Store). Add an Animator with two states or triggers: `Smile` and `Sad`. Create small animation clips for smile/sad body movement.
4. Add an AudioSource with the dialogue clip (local file). Assign it to `ReactionController.dialogueSource`.
5. Add `LipSyncFake` to the character, assign `faceMesh` if using blendshapes and set `mouthBlendShapeIndex`.
6. Hook the UI Play button to `ReactionController.playButton`.

Recording
- Use any screen recorder to capture a 1–2 minute walkthrough showing LoginScene → HumanoidScene and the full reaction sequence.

Deliverables
- Git repo with Assets/, Packages/, ProjectSettings/ (this project).
- 1–2 minute screen recording demonstrating the flow.

If you'd like, I can also add example animations and a small test setup to the repo. Ask me to proceed and I will add sample animator controller and simple animation clips created via script-friendly AnimationClips.
