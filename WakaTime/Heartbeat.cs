namespace WakaTime
{
    public class Heartbeat
    {
        public string Entity { get; set; }
        public int Lines { get; set; }
        public int LineNumber { get; set; }
        public string Timestamp { get; set; }
        public bool IsWrite { get; set; }

        /// <summary>
        /// It's a workaround for serialization.
        /// More details https://bit.ly/3mJB1mP
        /// </summary>
        public override string ToString()
        {
            return $"{{\"entity\":\"{Entity.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"," +
                $"\"lines-in-file\":{Lines}," +
                $"\"lineno\":{LineNumber}," +
                $"\"time\":{Timestamp}," +
                $"\"is_write\":{IsWrite.ToString().ToLower()}}}";
        }
    }
}
