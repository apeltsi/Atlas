namespace SolidCode.Atlas
{
    public static class Time
    {
        /// <summary>
        /// Time between the current frame and the start of the application. Measured in seconds
        /// </summary>

        public static double time { get; internal set; }
        /// <summary>
        /// Time between the current tick and the start of the application. Measured in seconds
        /// </summary>

        public static double tickTime { get; internal set; }

        /// <summary>
        /// The time between the current frame and the previous frame. Measured in seconds
        /// </summary>

        public static double deltaTime { get; internal set; }

        /// <summary>
        /// The time between the current tick and the previous tick. Measured in seconds
        /// </summary>

        public static double tickDeltaTime { get; internal set; }

    }
}