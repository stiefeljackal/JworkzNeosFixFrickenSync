namespace JworkzNeosMod.Models
{
    public struct UploadProgressState
    {
        /// <summary>
        /// The current stage of the upload.
        /// </summary>
        public string Stage { get; }

        /// <summary>
        /// THe current progress of the upload as a percentage.
        /// </summary>
        public float Progress { get; }

        /// <summary>
        /// Creates an instance of UploadProgressState that contains the current upload progres
        /// of the sync.
        /// </summary>
        /// <param name="stage">The current stage of the upload.</param>
        /// <param name="progress">The current progress of the upload as a percentage; defaults to 0f if none is provided.</param>
        public UploadProgressState(string stage, float progress = 0f)
        {
            Progress = progress;
            Stage = stage;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UploadProgressState)) { return false; }

            var rhs = (UploadProgressState)obj;

            return this.Stage == rhs.Stage && (this.Progress * 1000) == (rhs.Progress * 1000);
        }

        public static bool operator ==(UploadProgressState lhs, UploadProgressState rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(UploadProgressState lhs, UploadProgressState rhs)
            => !lhs.Equals(rhs);

        public static implicit operator UploadProgressState(string stage) =>
            new UploadProgressState(stage);
    }
}
