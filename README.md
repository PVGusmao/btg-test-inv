# API de Contas - BTG

CRUD de contas em .NET 8 com simulações locais de notificações entre áreas e cache em memória. Não utiliza banco de dados nem serviços AWS; os dados, o cache e as notificações são perdidos quando a aplicação é encerrada.

## Executar

Abra `ProjetoBtg.Api.csproj` no Visual Studio e pressione F5 ou Ctrl+F5. O navegador abre em `/swagger`.

## Fluxo de criação e confirmações

1. Crie uma conta usando `POST /api/contas`. A resposta terá `validacaoPrevencao: 0` e `validacaoCartao: 0`.
2. Um evento de ciclo de vida é enviado aos dois consumidores simulados: prevenção a fraudes e cartões.
3. Consulte `GET /api/contas/{id}/notificacoes` para ver as notificações de criação, atualização e remoção recebidas pelas áreas.
4. Simule a confirmação de cada área usando `POST /api/contas/{id}/confirmacoes/prevencao` e `POST /api/contas/{id}/confirmacoes/cartao`. Cada rota atualiza seu respectivo campo para `1` e também publica um evento de atualização.
5. Consulte `GET /api/contas/{id}` para ver os valores atualizados.

## Endpoints

- `GET /api/contas` - lista contas (cache local até o fim do dia UTC)
- `GET /api/contas/{id}` - consulta uma conta (cache local até o fim do dia UTC)
- `POST /api/contas` - cria uma conta e publica o evento local de criação
- `PUT /api/contas/{id}` - atualiza nome, CPF e status e publica o evento local de atualização
- `DELETE /api/contas/{id}` - remove uma conta e publica o evento local de remoção
- `GET /api/contas/{id}/notificacoes` - consulta notificações recebidas pelas áreas simuladas
- `POST /api/contas/{id}/confirmacoes/prevencao` - confirma a validação de prevenção a fraudes
- `POST /api/contas/{id}/confirmacoes/cartao` - confirma a validação da área de cartões

Quando uma consulta não encontra a informação no cache, a API busca no repositório em memória (que simula o banco local) e mantém o resultado até a próxima meia-noite UTC. O log informa `Cache hit` ou `Cache miss`. O cache é invalidado nas operações de criação, alteração, exclusão e confirmação, então as leituras seguintes já refletem a mudança. Esta simulação usa `IMemoryCache`; em produção, com múltiplas instâncias da API, seria necessário um cache compartilhado.

O CPF aceita os formatos `12345678909` ou `123.456.789-09` e valida os dígitos verificadores. O valor é armazenado somente com números. O status deve ser `Ativa` ou `Inativa` como texto.

## Testes unitários

O projeto `ProjetoBtg.Api.Tests` cobre validação de CPF, imutabilidade da entidade e publicação de eventos de atualização e remoção.

Exemplo de corpo para criar ou atualizar:

```json
{
  "nomeTitular": "Maria Silva",
  "cpf": "52998224725",
  "status": "Ativa"
}
```


