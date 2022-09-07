﻿using FasTnT.Infrastructure.Store;
using Microsoft.EntityFrameworkCore;

public static class DatabaseMigrator
{
    public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder application)
    {
        using var scope = application.ApplicationServices.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<EpcisContext>();

        context.Database.EnsureCreated();
        context.Database.Migrate();

        return application;
    }
}