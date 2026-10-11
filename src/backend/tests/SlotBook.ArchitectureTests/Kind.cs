namespace SlotBook.ArchitectureTests;

/// <summary>The role a project plays in the AD-2 layout, parsed from its name.</summary>
internal readonly record struct Kind(Role Role, string Module)
{
    public bool IsPure => Role is Role.KernelDomain or Role.KernelContracts or Role.ModuleDomain or Role.ModuleContracts;

    public static Kind Parse(string name)
    {
        switch (name)
        {
            case "SlotBook.Api":
                return new Kind(Role.Api, string.Empty);
            case "SlotBook.ServiceDefaults":
                return new Kind(Role.ServiceDefaults, string.Empty);
            case "SlotBook.SharedKernel.Domain":
                return new Kind(Role.KernelDomain, string.Empty);
            case "SlotBook.SharedKernel.Contracts":
                return new Kind(Role.KernelContracts, string.Empty);
            case "SlotBook.SharedKernel.Application":
                return new Kind(Role.KernelApplication, string.Empty);
            case "SlotBook.SharedKernel.Infrastructure":
                return new Kind(Role.KernelInfrastructure, string.Empty);
        }

        var parts = name.Split('.');
        if (parts is not ["SlotBook", "Modules", var module, ..])
        {
            return new Kind(Role.Unknown, string.Empty);
        }

        return parts switch
        {
            [_, _, _] => new Kind(Role.SimpleModule, module),
            [_, _, _, "Domain"] => new Kind(Role.ModuleDomain, module),
            [_, _, _, "Contracts"] => new Kind(Role.ModuleContracts, module),
            [_, _, _, "Application"] => new Kind(Role.ModuleApplication, module),
            [_, _, _, "Infrastructure"] => new Kind(Role.ModuleInfrastructure, module),
            _ => new Kind(Role.Unknown, string.Empty),
        };
    }
}
