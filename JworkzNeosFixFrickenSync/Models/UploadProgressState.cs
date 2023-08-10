namespace JworkzNeosMod.Models
{
    public struct UploadProgressState
    {
        /// <summary>
        /// The current stage of the upload.
        /// </summary>
        public string Stage { get; }

        /// <summary>
        /// The current progress of the upload as a percentage.
        /// </summary>
        public float Progress { get; }

        /// <summary>
        /// An indicator to tell if the sync was successful, a failure, or still in progress. true for
        /// successful, false for failure, and null for in progress.
        /// </summary>
        public UploadProgressIndicator Indicator { get; }

        /// <summary>
        /// Creates an instance of UploadProgressState that contains the current upload progres
        /// of the sync.
        /// </summary>
        /// <param name="stage">The current stage of the upload.</param>
        /// <param name="progress">The current progress of the upload as a percentage; defaults to 0f if none is provided.</param>
        public UploadProgressState(string stage, UploadProgressIndicator indicator = UploadProgressIndicator.InProgress, float progress = 0f)
        {
            Stage = stage;
            Indicator = indicator;

            switch(indicator)
            {
                case UploadProgressIndicator.Success:
                case UploadProgressIndicator.Canceled:
                case UploadProgressIndicator.Failure:
                    Progress = 1f;
                    break;
                default:
                    Progress = progress;
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UploadProgressState)) { return false; }

            var rhs = (UploadProgressState)obj;

            return this.Stage == rhs.Stage && (this.Progress * 1000) == (rhs.Progress * 1000) && this.Indicator == rhs.Indicator;
        }

        public override int GetHashCode() =>
            Stage.GetHashCode() * 100000 + (int)(Progress * 1000) + (int)Indicator * 10000;

        public static bool operator ==(UploadProgressState lhs, UploadProgressState rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(UploadProgressState lhs, UploadProgressState rhs)
            => !lhs.Equals(rhs);

        public static implicit operator UploadProgressState(string stage) =>
            new UploadProgressState(stage);
    }
}
