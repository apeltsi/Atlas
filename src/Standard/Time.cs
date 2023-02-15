namespace SolidCode.Atlas
{
    public static class Time
    {
        /// <summary>
        /// Time between the current frame and the start of the application. Measured in seconds
        /// </summary>

        public static double time { get; internal set; }
        /// <summary>
        /// Time between the current fixed update and the start of the application. Measured in seconds
        /// </summary>

        public static double fixedTime { get; internal set; }

        /// <summary>
        /// The time between the current frame and the previous frame. Measured in seconds
        /// </summary>

        public static double deltaTime { get; internal set; }

        /// <summary>
        /// The time between the current fixed upadte and the previous fixed update. Measured in seconds
        /// </summary>

        public static double fixedDeltaTime { get; internal set; }

    }
}