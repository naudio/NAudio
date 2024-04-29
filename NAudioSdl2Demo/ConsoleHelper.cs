namespace NAudioSdl2Demo
{
    public static class ConsoleHelper
    {
        private static Dictionary<string, (int, int)?> _cursorPositions;

        static ConsoleHelper()
        {
            _cursorPositions = new Dictionary<string, (int, int)?>();
        }

        public static void LockCursorPosition(string key)
        {
            _cursorPositions.TryAdd(key, null);
            _cursorPositions[key] ??= Console.GetCursorPosition();
            Console.SetCursorPosition(_cursorPositions[key]!.Value.Item1, _cursorPositions[key]!.Value.Item2);
        }
    }
}
