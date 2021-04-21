using System;

namespace DevHawk.Registrar.Cli
{
    class ConsoleColorManager : IDisposable
    {
        readonly ConsoleColor originalForegroundColor;
        readonly ConsoleColor? originalBackgroundColor;

        public ConsoleColorManager(ConsoleColor foregroundColor, ConsoleColor? backgroundColor = null)
        {
            originalForegroundColor = Console.ForegroundColor;
            originalBackgroundColor = backgroundColor.HasValue ? Console.BackgroundColor : null;

            Console.ForegroundColor = foregroundColor;
            if (backgroundColor.HasValue)
            {
                Console.BackgroundColor = backgroundColor.Value;
            }
        }

        public void Dispose()
        {
            Console.ForegroundColor = originalForegroundColor;
            if (originalBackgroundColor.HasValue)
            {
                Console.BackgroundColor = originalBackgroundColor.Value;
            }
        }

        public static IDisposable SetColor(ConsoleColor foregroundColor, ConsoleColor? backgroundColor = null)
        {
            return new ConsoleColorManager(foregroundColor, backgroundColor);
        }
    }
}
