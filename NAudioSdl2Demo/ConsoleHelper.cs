namespace NAudioSdl2Demo
{
    public static class ConsoleHelper
    {
        private static readonly Dictionary<string, (int, int)?> cursorPositions;

        static ConsoleHelper()
        {
            cursorPositions = new Dictionary<string, (int, int)?>();
        }

        public static void LockCursorPosition(string key)
        {
            cursorPositions.TryAdd(key, null);
            cursorPositions[key] ??= Console.GetCursorPosition();
            Console.SetCursorPosition(cursorPositions[key]!.Value.Item1, cursorPositions[key]!.Value.Item2);
        }
    }
}
