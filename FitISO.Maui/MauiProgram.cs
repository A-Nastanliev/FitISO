using CommunityToolkit.Maui;
using FitISO.Data;
using FitISO.Maui.ViewModels;
using FitISO.Maui.Views;
using FitISO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitISO.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseNaluLayouts()
                .UseMauiCommunityToolkit()
                .UseNaluTabBar()
                .UseNaluLayouts()
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

            return app;
        }
    }
}
