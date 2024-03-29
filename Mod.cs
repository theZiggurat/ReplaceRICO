using Colossal.Logging;
using Game;
using Game.Modding;

namespace ReplaceRICO
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(ReplaceRICO)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info("Starting ReplaceRICO");
            updateSystem.UpdateBefore<UpdateRicoSystem>(SystemUpdatePhase.MainLoop);
        }

        public void OnDispose()
        {
            log.Info("Disposing ReplaceRICO");
        }
    }
}
