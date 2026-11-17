# Evolve development standards

These permanent standards apply to all development tasks throughout this
repository, regardless of the module or roadmap being worked on.

- Repository: `fgabo404/Evolve`
- Working branch: `architecture`
- Architecture: `architecture.MD`
- Current UI roadmap: `game/roadmap.md`

## Language

- Use Spanish for all communication with the project owner, including
  explanations, progress reports, implementation summaries, audit responses
  and commit descriptions presented in chat.
- Use English for all internal project content: source code, identifiers,
  class, method, variable and file names, code comments, technical
  documentation, roadmaps, internal messages, logs and commit messages.
- Keep actual Git commit messages in English; describe their purpose in Spanish
  when reporting them to the project owner.
- Player-facing text may use other languages according to the game's design.
- Migrate existing non-English documentation only when separately authorized;
  do not translate older documents as an incidental part of another task.

## Code documentation

- Use only `//` for code comments. Do not use block comments or XML
  documentation comments such as `/// <summary>`.
- Document the purpose of classes, methods and important logic blocks.
- Explain why code exists or how non-obvious logic works.
- Avoid unnecessary or redundant comments that merely repeat the code.
- Keep comments accurate and updated whenever the related code changes.

## Naming

- Use descriptive, meaningful English names and follow C# naming conventions.
- Avoid generic filenames such as `Program.cs`, `Manager.cs`, `Helper.cs` or
  `Utils.cs` when a more precise name is possible.
- Preserve the approved lowercase directory names: `definitions/`, `engine/`,
  `state/`, `saves/`, `game/` and `tests/`.

## Architecture

- Read and respect `architecture.MD` before implementation.
- Keep modules independent according to their approved responsibilities.
- Keep simulation rules out of `game/`; the simulation must be able to operate
  without starting the graphical interface.
- Avoid unnecessary dependencies, duplicated logic and premature abstractions.
- Do not rewrite existing implementations without authorization.
- Respect existing work and limit changes to the authorized task and modules.

## Roadmap workflow

- Identify and read the applicable roadmap and relevant module documentation
  before development. For UI work, read `game/readme.md` and `game/roadmap.md`.
- Implement only the explicitly authorized stage, one stage at a time.
- Do not advance to another stage without approval.
- Do not modify the architecture or roadmap without authorization.
- The Architect designs, Dev implements, the Auditor verifies, and the project
  owner approves decisions and accepts the result.

## Delivery

- Identify every modified file, including additions, renames and deletions.
- Provide the commit message and complete commit SHA.
- Provide PowerShell commands for the responsible person or Auditor to validate
  the change.
- Respect task-specific restrictions on builds, tests and execution.
- Never claim that tests passed without execution evidence. State what was
  actually checked and which validation remains pending.
