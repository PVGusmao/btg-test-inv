# API de contas - BTG

API de gerenciamento de contas em .NET 8. O projeto usa SQLite local para persistir contas e notificações, `IMemoryCache` para reduzir leituras repetidas e consumidores em memória para simular a comunicação com as áreas de prevenção a fraudes e cartões.

## Executar

Abra `ProjetoBtg.Api.csproj` no Visual Studio e pressione F5 ou Ctrl+F5. A aplicação abre o Swagger em `/swagger`.

Na primeira inicialização, o Entity Framework Core aplica as migrations e cria `accounts.db` na pasta do projeto. Não é necessário instalar ou iniciar um servidor de banco de dados, nem configurar variáveis de ambiente. O arquivo SQLite está no `.gitignore`, então os dados locais não são enviados ao Git.

Para adicionar uma migration depois de alterar o modelo:

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaAlteracao --project ProjetoBtg.Api.csproj
```

## Persistência e exclusão lógica

A tabela `Accounts` armazena o ID, nome do titular, CPF, status, confirmações das áreas e a coluna `Desativado` (`0` ou `1`). Contas novas começam com `Desativado = 0`. A rota `DELETE /api/contas/{id}` faz uma exclusão lógica: muda a coluna para `1` diretamente no SQLite e mantém o registro no banco.

As consultas da lista e por identificador, as confirmações e a verificação de CPF consideram apenas contas com `Desativado = 0`. O índice único do CPF também se aplica somente às contas ativas. Assim, um registro desativado não aparece nas consultas normais e seu CPF pode ser usado por uma nova conta.

A consulta de uma conta ativa usa a rota existente e aceita ID ou CPF. A reativação também aceita ID ou CPF e procura registros desativados. Se o CPF corresponder a mais de um registro histórico, a API pede o ID para evitar reativar a conta errada.

A tabela `AreaNotifications` guarda o ID da conta, área, operação e horário de cada notificação. Ela não tem uma chave estrangeira para `Accounts`, de modo que o histórico continua disponível depois da desativação. Contas e notificações persistem após reiniciar a aplicação.

## Cache

O cache é uma cópia temporária na memória da aplicação, separada do SQLite. Nas rotas `GET /api/contas` e `GET /api/contas/{idOuCpf}`, um **cache hit** retorna a cópia sem consultar o banco. Em um **cache miss**, a API busca no SQLite e guarda o resultado até a próxima meia-noite UTC.

Criação, atualização, desativação, reativação e confirmações invalidam a lista e as entradas de consulta por identificador. A próxima consulta busca o estado atual no SQLite e renova o cache. O cache é perdido quando a aplicação é encerrada; as contas e notificações continuam no arquivo SQLite.

## Eventos simulados

Na criação, atualização, desativação, reativação ou confirmação, a API publica um evento local para dois consumidores simulados: prevenção a fraudes e cartões. Cada consumidor grava sua notificação em `AreaNotifications`. Essa simulação roda dentro do processo da API; não utiliza um serviço externo de mensageria.

As rotas de confirmação salvam a alteração, invalidam o cache e retornam uma frase de confirmação.

## Endpoints

- `GET /api/contas` - lista contas ativas.
- `GET /api/contas/{idOuCpf}` - consulta uma conta não desativada por ID ou CPF exato.
- `POST /api/contas` - cria uma conta e publica o evento de criação.
- `PUT /api/contas/{id}` - atualiza nome, CPF ou status e publica o evento de atualização.
- `DELETE /api/contas/{id}` - desativa a conta (`Desativado = 1`) e publica o evento de remoção.
- `POST /api/contas/{idOuCpf}/reativacao` - reativa uma conta desativada (`Desativado = 0`) por ID ou CPF exato. Retorna `409 Conflict` se o CPF já estiver sendo usado por outra conta ativa ou se houver vários registros históricos com o mesmo CPF.
- `GET /api/contas/{id}/notificacoes` - consulta o histórico de notificações da conta, inclusive após desativação.
- `POST /api/contas/{id}/confirmacoes/prevencao` - confirma a verificação da área de prevenção a fraudes.
- `POST /api/contas/{id}/confirmacoes/cartao` - confirma a verificação da área de cartões.

O CPF aceita `12345678909` ou `123.456.789-09`, valida os dígitos verificadores e é armazenado somente com números. O status aceito é `Ativa` ou `Inativa`. Use CPFs fictícios para demonstrar o projeto.

As rotas de consulta e reativação por CPF exigem CPF válido. Se mais de uma conta histórica tiver o mesmo CPF, a reativação retorna `409 Conflict`; nesse caso, use o ID da conta para selecionar qual registro reativar.

## Logs

Os logs indicam quando uma leitura veio do cache ou do SQLite, quando uma conta foi criada, atualizada ou desativada, quando notificações foram gravadas/consultadas e quando o cache foi invalidado. O CPF não é incluído nos logs.

## Testes

Os testes unitários podem ser executados com:

```powershell
dotnet test .\ProjetoBtg.Api.Tests\ProjetoBtg.Api.Tests.csproj
```

Exemplo de corpo para criar ou atualizar uma conta:

```json
{
  "nomeTitular": "Maria Silva",
  "cpf": "52998224725",
  "status": "Ativa"
}
```
