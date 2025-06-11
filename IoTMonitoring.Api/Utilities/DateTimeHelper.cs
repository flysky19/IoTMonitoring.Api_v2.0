namespace IoTMonitoring.Api.Utilities
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo KstTimeZone;

        static DateTimeHelper()
        {
            try
            {
                // Windows (로컬 개발환경)
                KstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            }
            catch
            {
                try
                {
                    // Linux/Unix (Azure App Service Linux)
                    KstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
                }
                catch
                {
                    // 실패 시 수동 생성
                    KstTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "KST",
                        TimeSpan.FromHours(9),
                        "Korea Standard Time",
                        "Korea Standard Time"
                    );
                }
            }
        }

        // 항상 한국 시간 반환 (로컬이든 Azure든 동일하게 작동)
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KstTimeZone);

        // DateTime을 한국 시간으로 변환
        public static DateTime ToKoreanTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, KstTimeZone);
        }
    }
}
