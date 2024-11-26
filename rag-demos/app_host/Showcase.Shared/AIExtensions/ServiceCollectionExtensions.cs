using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AIExtensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIToolRegistry(this IServiceCollection services, IEnumerable<AIFunction>? tools = default)
    {
        return services.AddSingleton(new AIToolRegistry(tools ?? []));
    }

    public static IServiceCollection AddVoiceClient(this IServiceCollection services)
    {
        return services.AddScoped<IVoiceClient, VoiceClient>();
    }

}
