# API de Contas - BTG

CRUD de contas em .NET 8 com simulações locais de notificações entre áreas e cache em memória. Não utiliza banco de dados nem serviços AWS; os dados e as notificações são perdidos quando a aplicação é encerrada.

## Executar

Abra `ProjetoBtg.Api.csproj` no Visual Studio e pressione F5 ou Ctrl+F5. O navegador abre em `/swagger`.

## Fluxo de criação e confirmações

1. Crie uma conta usando `POST /api/contas`. A resposta terá `validacaoPrevencao: 0` e `validacaoCartao: 0`.
2. O evento interno `ContaCriada` é enviado aos dois consumidores simulados: prevenção a fraudes e cartões.
3. Consulte `GET /api/contas/{id}/notificacoes` para ver que as duas áreas receberam a notificação.
4. Simule a confirmação de cada área usando `POST /api/contas/{id}/confirmacoes/prevencao` e `POST /api/contas/{id}/confirmacoes/cartao`. Cada rota atualiza seu respectivo campo para `1`.
5. Consulte `GET /api/contas/{id}` para ver os valores atualizados.

## Endpoints

- `GET /api/contas` - lista contas (cache local por 2 minutos)
- `GET /api/contas/{id}` - consulta uma conta (cache local por 2 minutos)
- `POST /api/contas` - cria uma conta e publica o evento local
- `PUT /api/contas/{id}` - atualiza nome, CPF e status
- `DELETE /api/contas/{id}` - remove uma conta
- `GET /api/contas/{id}/notificacoes` - consulta notificações recebidas pelas áreas simuladas
- `POST /api/contas/{id}/confirmacoes/prevencao` - confirma a validação de prevenção a fraudes
- `POST /api/contas/{id}/confirmacoes/cartao` - confirma a validação da área de cartões

Quando uma consulta não encontra a informação no cache, a API busca no repositório em memória (que simula o banco local) e guarda o resultado com expiração após 5 minutos sem acesso e limite máximo de 10 minutos. O log informa `Cache hit` ou `Cache miss`. O cache é invalidado nas operações de criação, alteração, exclusão e confirmação. Esta simulação usa `IMemoryCache`; em produção, com múltiplas instâncias da API, seria necessário um cache compartilhado.

Exemplo de corpo para criar ou atualizar:

```json
{
  "nomeTitular": "Maria Silva",
  "cpf": "12345678900",
  "status": "Ativa"
}
```


