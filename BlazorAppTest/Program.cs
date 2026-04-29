using BlazorAppTest.Components;
using BlazorAppTest.Configurator;
using BlazorAppTest.Configurator.AppBootstrapper;
using BlazorAppTest.Domain;
using BlazorAppTest.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace BlazorAppTest
{
    public class Program
    {
        public static void Main(string[] args)
            => new ModuleBootstrapper().Run(args);
    }
}
