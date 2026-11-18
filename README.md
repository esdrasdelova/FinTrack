# FinTrack

Uma plataforma completa de gestão financeira desenvolvida com ASP.NET Core MVC, C#, Entity Framework Core, SQL Server, HTML, CSS e JavaScript.

## Estrutura do repositório

```
FinTrack/
├── README.md
├── LICENSE
├── .gitignore
├── FinTrack.sln
└── src/
    └── FinTrack/
        ├── Controllers/
        ├── Data/
        ├── Extensions/
        ├── Models/
        ├── Properties/
        ├── Repositories/
        ├── Services/
        ├── ViewModels/
        ├── Views/
        ├── wwwroot/
        ├── FinTrack.csproj
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json
```

O código-fonte vive em `src/FinTrack`. A solução `FinTrack.sln` na raiz referencia esse projeto, permitindo compilar e restaurar dependências de qualquer diretório do repositório.

## Tecnologias

- ASP.NET Core MVC com Razor Views (.NET 10)
- ASP.NET Core Identity (autenticação, autorização e isolamento por usuário)
- Entity Framework Core com SQL Server (LocalDB em desenvolvimento)
- Chart.js, Bootstrap 5, Bootstrap Icons e SweetAlert2 (bibliotecas servidas localmente)
- ClosedXML (exportação Excel) e QuestPDF (exportação PDF)
- Serilog (logs em arquivo e console)

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) ou superior
- SQL Server LocalDB (`MSSQLLocalDB`) — já vem com o Visual Studio ou pode ser instalado separadamente
- [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet) (somente se precisar criar/aplicar migrações manualmente)

## Como executar

Na raiz do repositório:

```bash
dotnet restore
dotnet build
dotnet run --project src/FinTrack/FinTrack.csproj
```

Por padrão, a aplicação usa os perfis definidos em `src/FinTrack/Properties/launchSettings.json` (`http://localhost:5226` e `https://localhost:7194`). Para escolher uma porta manualmente:

```bash
dotnet run --project src/FinTrack/FinTrack.csproj --urls http://localhost:5080
```

Na primeira execução, o banco `FinTrackDb` é criado, as migrações são aplicadas e os dados fictícios de demonstração são semeados automaticamente.

Para atualizar o banco manualmente após alterar o modelo:

```bash
dotnet ef migrations add NomeDaMigracao --project src/FinTrack/FinTrack.csproj
dotnet ef database update --project src/FinTrack/FinTrack.csproj
```

## Conta de demonstração

Todos os dados de demonstração pertencem ao usuário fictício abaixo e são identificados por um banner na interface:

- **E-mail:** `demo@fintrack.app`
- **Senha:** `Demo@1234`

Cadastre-se para utilizar seus próprios dados. Cada usuário enxerga somente suas informações.

## Observações

- Logs são gravados em `src/FinTrack/logs/`.
- Anexos enviados nas transações ficam em `src/FinTrack/wwwroot/uploads/` (ignorados pelo Git).
- A connection string e as credenciais de demonstração ficam em `src/FinTrack/appsettings.json`.

## Licença

Distribuído sob a licença MIT. Consulte o arquivo [LICENSE](LICENSE).
