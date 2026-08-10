# Programmer's Guide — Localization

Route Service supports multiple locales. The localization layer lives in:

- `Application/Common/Interfaces/ILocalizationService.cs`
- `Infrastructure/Localization/ResourceLocalizationService.cs`
- `Infrastructure/Localization/Resources/Messages.resx` (English, default)
- `Infrastructure/Localization/Resources/Messages.bn.resx` (Bangla)

## How it works
`ResourceLocalizationService` uses `System.Resources.ResourceManager` to resolve keys.
The `LocalizationMiddleware` reads the `lang` query parameter or `Accept-Language` header
and stores the resolved locale in `HttpContext.Items["Locale"]`.

## Adding a new language
1. Add a new `.resx` file next to `Messages.resx` named `Messages.<culture>.resx` (e.g. `Messages.fr.resx`).
2. Translate the keys.
3. No code changes required — `ResourceManager` handles culture fallback automatically.

## Usage in validators
```csharp
RuleFor(x => x.Code).NotEmpty().WithMessage(localization.GetString("Route.CodeRequired"));
```

## Usage in handlers
```csharp
return Result<RouteDto>.Failure(new Error("InvalidRoute", localization.GetString("Route.OriginDestinationSame")));
```
