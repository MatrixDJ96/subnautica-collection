namespace BetterSubnautica.Utility
{
    public static class GraphicsUtility
    {
        public static void OnQualityLevelChanged()
        {
            if (GraphicsUtil.onQualityLevelChanged != null)
            {
                GraphicsUtil.onQualityLevelChanged();
            }
        }
    }
}
