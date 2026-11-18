namespace FinTrack.Models;

/// <summary>
/// Contrato aplicado a todas as entidades financeiras. Garante que todo registro
/// pertença a um usuário, permitindo o isolamento no repositório.
/// </summary>
public interface IUserOwnedEntity
{
    int Id { get; set; }
    string UserId { get; set; }
}
