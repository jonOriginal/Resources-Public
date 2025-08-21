using System.Collections.Immutable;
using Analysers.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analysers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableConfigPropertiesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "NI001",
        "Config Interface Property Must Be Nullable",
        "Property '{0}' in interface '{1}' must be nullable",
        "CodeStyle",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
    }

    private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
    {
        var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

        var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration);
        if (interfaceSymbol == null || !HasConfigAttribute(interfaceSymbol)) return;

        foreach (var member in interfaceDeclaration.Members)
        {
            if (member is not PropertyDeclarationSyntax property) continue;
            var propertyType = property.Type;

            if (propertyType is NullableTypeSyntax) continue;

            var propertySymbol = context.SemanticModel.GetTypeInfo(propertyType).Type;
            if (propertySymbol is { IsReferenceType: true } && !propertyType.ToString().EndsWith("?"))
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    property.Identifier.GetLocation(),
                    property.Identifier.Text,
                    interfaceDeclaration.Identifier.Text));
        }
    }

    private static bool HasConfigAttribute(INamedTypeSymbol interfaceSymbol)
    {
        foreach (var attribute in interfaceSymbol.GetAttributes())
            if (attribute.AttributeClass?.Name == nameof(ConfigAttribute) ||
                attribute.AttributeClass?.ToDisplayString() == nameof(ConfigAttribute))
                return true;

        return false;
    }
}