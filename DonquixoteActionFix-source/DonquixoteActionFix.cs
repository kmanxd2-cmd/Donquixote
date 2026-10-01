using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Modding;

namespace DonquixoteActionFix;

[ModInitializer(nameof(Initialize))]
public partial class ModEntry : Node
{
    private const string HarmonyId = "local.donquixote.standalone.actionfix";
    private const string OriginalModId = "Donquixote";
    private const string OriginalAssemblyResource = "DonquixoteActionFix.OriginalDonquixote.dll";

    public static void Initialize()
    {
        try
        {
            var originalAssembly = LoadOriginalAssembly();
            var harmony = new Harmony(HarmonyId);

            AssociateOriginalAssembly(originalAssembly);
            harmony.PatchAll(originalAssembly);
            PatchDefendAnimationWait(harmony, originalAssembly);
            InvokeOriginalModInitializers(originalAssembly);

            GD.Print("[DonquixoteStandalone] Original character loaded with the Defend animation cancellation fix.");
        }
        catch (Exception exception)
        {
            GD.PushError($"[DonquixoteStandalone] Could not initialize standalone character mod: {exception}");
        }
    }

    private static Assembly LoadOriginalAssembly()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(OriginalAssemblyResource)
            ?? throw new FileNotFoundException($"Embedded original mod assembly not found: {OriginalAssemblyResource}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Assembly.Load(buffer.ToArray());
    }

    private static void AssociateOriginalAssembly(Assembly originalAssembly)
    {
        var modManager = typeof(ModEntry).Assembly.GetType("MegaCrit.Sts2.Core.Modding.ModManager", throwOnError: true)!;
        var associate = AccessTools.Method(modManager, "AssociateAssemblyWithMod", new[] { typeof(string), typeof(Assembly) })
            ?? throw new MissingMethodException(modManager.FullName, "AssociateAssemblyWithMod");
        associate.Invoke(null, new object[] { OriginalModId, originalAssembly });
    }

    private static void PatchDefendAnimationWait(Harmony harmony, Assembly originalAssembly)
    {
        var stateMachine = originalAssembly.GetType(
            "DonquixoteMod.DonquixoteDefend+<OnPlay>d__7",
            throwOnError: true)!;
        var target = AccessTools.Method(stateMachine, "MoveNext")
            ?? throw new MissingMethodException(stateMachine.FullName, "MoveNext");
        var transpiler = AccessTools.Method(typeof(DonquixoteDefendAnimationWaitPatch), nameof(DonquixoteDefendAnimationWaitPatch.Transpiler))
            ?? throw new MissingMethodException(nameof(DonquixoteDefendAnimationWaitPatch.Transpiler));

        harmony.Patch(target, transpiler: new HarmonyMethod(transpiler));
    }

    private static void InvokeOriginalModInitializers(Assembly originalAssembly)
    {
        var modManager = typeof(ModEntry).Assembly.GetType("MegaCrit.Sts2.Core.Modding.ModManager", throwOnError: true)!;
        var initializeType = AccessTools.Method(modManager, "CallModInitializer", new[] { typeof(Type) })
            ?? throw new MissingMethodException(modManager.FullName, "CallModInitializer");

        foreach (var type in originalAssembly.GetTypes())
            initializeType.Invoke(null, new object[] { type });
    }
}

internal static class DonquixoteDefendAnimationWaitPatch
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var replacement = AccessTools.Method(typeof(DonquixoteDefendAnimationWaitPatch), nameof(CompletedAnimationTask))
            ?? throw new MissingMethodException(nameof(CompletedAnimationTask));
        var replacements = 0;

        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && IsCastAnimationTask(method))
            {
                // The matched call is static. Replacing only its operand preserves opcode and metadata.
                instruction.operand = replacement;
                replacements++;
            }

            yield return instruction;
        }

        if (replacements != 1)
        {
            throw new InvalidOperationException(
                $"Expected one Donquixote Cast animation call, replaced {replacements}. The mod/game version may have changed.");
        }
    }

    private static bool IsCastAnimationTask(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.IsStatic &&
               method.Name == "TriggerAnim" &&
               method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(Creature) &&
               parameters[1].ParameterType == typeof(string) &&
               parameters[2].ParameterType == typeof(float);
    }

    private static Task CompletedAnimationTask(Creature creature, string animation, float delay) => Task.CompletedTask;
}
