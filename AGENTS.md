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
- Write development prompts in Spanish as well.
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
- Every C# source file must begin with an English `//` comment explaining its
  main purpose, responsibility within the system and relationship to other
  components when relevant. Place this header before `using` directives,
  namespaces or top-level statements, including in entry points and tests.
- Apply this convention to all new C# files and keep existing file headers
  accurate when changing their responsibilities. Avoid repeating the header
  as a redundant class comment.
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

## Git publication

After completing an explicitly authorized implementation task:

1. Review every modified file and confirm that the changes stay within scope.
2. Create a commit with a descriptive English message.
3. Automatically execute `git push origin architecture`, including any pending
   authorized local commits.
4. Verify that publication succeeded by checking that the complete local commit
   SHA matches the `architecture` branch SHA on GitHub.
5. Report the commit message and complete SHA to the project owner in Spanish,
   preserving the actual English commit message when quoting it.

Never use force push or overwrite remote changes. If pushing fails, report the
issue instead of forcing synchronization. Do not claim publication succeeded
without remote verification.

## Game versioning

- The root `VERSION` file is the single source of truth for the displayed game
  version. Use `vMajor.Minor.Patch`, with at least two patch digits.
- Initialize this system at `v0.0.01`. Every subsequent authorized commit that
  changes the project must increment the patch value: `v0.0.02`, `v0.0.03`, and
  so on. Update `VERSION` in the same commit, including documentation changes.
- Derive .NET version properties from `VERSION`; never duplicate hardcoded
  game version strings in source code or project configuration.
- Preserve the exact display string, including `v` and leading zeros, in
  `AssemblyInformationalVersion`. Read that metadata for the in-game version.
- Numeric assembly and file versions may be normalized for .NET compatibility;
  never use them as the display string or append a Git SHA to the display value.
- Keep the displayed version synchronized with `VERSION` on every build.
