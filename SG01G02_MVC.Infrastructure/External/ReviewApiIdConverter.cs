using Microsoft.Extensions.Logging;

namespace SG01G02_MVC.Infrastructure.External
{
    public static class ReviewApiIdConverter
    {
        public static int? Parse(string? externalId, ILogger? logger = null)
        {
            if (int.TryParse(externalId, out var id))
                return id;

            logger?.LogWarning("Could not parse external productId: {ExternalId}", externalId ?? "null");
            return null;
        }
    }
}