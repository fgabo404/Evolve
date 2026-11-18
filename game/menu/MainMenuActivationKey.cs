// Identifies Enter and Space independently of native window APIs.
// Lets the interaction model track matching key releases without OpenTK.

namespace Evolve.Game;

internal enum MainMenuActivationKey
{
    Enter,
    Space
}
