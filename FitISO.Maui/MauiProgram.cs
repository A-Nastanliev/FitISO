using CommunityToolkit.Maui;
using FitISO.Data;
using FitISO.Maui.ViewModels;
using FitISO.Maui.Views;
using FitISO.Services;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FitISO.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseSkiaSharp()
                .UseLiveCharts()
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseNaluTabBar()
                .UseNaluLayouts()
                .UseLiveCharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Round.otf", "MaterialRound");
                });

            var dbPath = App.DatabasePath;

            builder.Services.AddDbContextFactory<FitDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<ExerciseService>();
            builder.Services.AddSingleton<SetService>();
            builder.Services.AddSingleton<WorkoutExerciseService>();
            builder.Services.AddSingleton<WorkoutService>();

            builder.Services.AddSingleton<ExerciseCollection>();
            builder.Services.AddSingleton<ExercisePageViewModel>();

            builder.Services.AddTransient<AddExercisePopupPage>();
            builder.Services.AddTransient<AddExercisePopupViewModel>();

            builder.Services.AddTransient<ExerciseDetailsPopupPage>();
            builder.Services.AddTransient<ExerciseDetailsPopupViewModel>();

            builder.Services.AddSingleton<SettingsPageViewModel>();

            builder.Services.AddTransient<WorkoutFormPage>();
            builder.Services.AddTransient<WorkoutFormViewModel>();

            builder.Services.AddSingleton<WorkoutTemplatesViewModel>();

            builder.Services.AddSingleton<ActiveWorkoutViewModel>();

            builder.Services.AddTransient<SelectExercisePopupPage>();
            builder.Services.AddTransient<SelectExercisePopupViewModel>();

            builder.Services.AddSingleton<HistoryPageViewModel>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FitDbContext>>();
                using var db = factory.CreateDbContext();
                db.Database.Migrate();
            }

#if ANDROID
            Microsoft.Maui.Handlers.ToolbarHandler.Mapper.AppendToMapping("NoInset", (handler, view) =>
            {
                handler.PlatformView.ContentInsetStartWithNavigation = 0;
                handler.PlatformView.SetContentInsetsAbsolute(0, 0);
            });
#endif

#if ANDROID
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            });
#endif
            app.Services.GetRequiredService<ActiveWorkoutViewModel>();

            return app;
        }
    }
}
