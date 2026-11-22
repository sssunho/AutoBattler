using System.Reflection;
using RaggedBanner.Combat.Unity;

namespace RaggedBanner.Combat.Tests
{
    static class BattlePresentationTestUtility
    {
        public static object GetDriver(BattleRunner runner)
        {
            return typeof(BattleRunner).GetProperty("Presentation", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(runner);
        }

        public static object Invoke(BattleRunner runner, string method, params object[] arguments)
        {
            if (method == "UpdateCurrentAction")
            {
                object simulation = typeof(BattleRunner).GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(runner);
                bool canContinue = (bool)typeof(BattleRunner)
                    .GetMethod("CanContinuePresentation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(runner, new[] { simulation });

                if (!canContinue)
                {
                    return null;
                }

                AdvanceAnimations(runner, (float)arguments[0]);
            }

            object driver = GetDriver(runner);
            return driver.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(driver, arguments);
        }

        public static void AdvanceAnimations(BattleRunner runner, float deltaTime)
        {
            typeof(BattleRunner).GetMethod("AdvanceAnimations", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runner, new object[] { deltaTime });
        }

        public static void DisableAndReenable(BattleRunner runner)
        {
            FieldInfo versionField = typeof(BattleRunner).GetField("_presentationVersion", BindingFlags.Instance | BindingFlags.NonPublic);
            uint version = (uint)versionField.GetValue(runner);
            runner.enabled = false;
            if ((uint)versionField.GetValue(runner) == version)
            {
                typeof(BattleRunner).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(runner, null);
            }

            runner.enabled = true;
        }
    }
}
