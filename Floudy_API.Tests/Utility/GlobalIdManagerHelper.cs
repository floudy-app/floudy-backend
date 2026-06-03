using System.Reflection;
using Floudy.API.Utility;

namespace Floudy.API.Tests.Helpers;

internal static class GlobalIdManagerHelper
{
    internal static void Reset()
    {
        var type = typeof(GlobalIdManager);

        GlobalIdManager.BaseValue = 1;

        var available_field = type.GetField("available", BindingFlags.NonPublic | BindingFlags.Static)!;
        var available = (Stack<long>)available_field.GetValue(null)!;

        available.Clear();
        available.Push(1);

        var available_hash_field = type.GetField("available_hash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var available_hash = (HashSet<long>)available_hash_field.GetValue(null)!;

        available_hash.Clear();
        available_hash.Add(1);
    }
}