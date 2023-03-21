﻿using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using eoj12.DCS.Toolkit.Data;
using Radzen;
using CommunityToolkit.Maui;

namespace eoj12.DCS.Toolkit;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddScoped<DialogService>();

        //builder.Services.AddSingleton<DialogService>();

        return builder.Build();
	}
}
