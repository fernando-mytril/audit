# Mytril Material ID

Mede tubos, perfis e chapas só com a câmera do celular, calcula o **peso real pela tabela do fabricante** e mostra a **economia** contra o peso cobrado ou declarado.

* Projeto no Linear: **Material ID — Identificação de materiais por foto** (P-MYT-5)
* Decisões e arquitetura: documento **"Refinamento do MVP — Decisões e arquitetura"** no projeto (D1 a D39)

## Estrutura

```
material-id/
├── backend/                         .NET 10 — monólito modular (D4)
│   ├── src/Mytril.MaterialId.SharedKernel   máquina de estados genérica, empresa, exceções
│   ├── src/Mytril.MaterialId.Catalogo       catálogo por fabricante + cálculo de peso (schema "catalogo")
│   ├── src/Mytril.MaterialId.Conferencias   conferências e amarrados, máquinas de estado (schema "conferencias")
│   ├── src/Mytril.MaterialId.Api            host: JWT do Mytril Identity, endpoints, Problem Details
│   └── tests/Mytril.MaterialId.Tests        domínio, catálogo, multiempresa e API ponta a ponta
├── web/                             Angular 22 PWA, mobile first, sem design system, AG Grid Community
└── tools/gerar_catalogo_seed.py     gera a carga do catálogo a partir das tabelas dos fabricantes
```

## Rodando local

**Tudo com Docker:** `docker compose up --build` → front em http://localhost:8084 e API em http://localhost:8085/scalar.

**Sem Docker:**

```bash
# 1. PostgreSQL na porta 5437 (ou ajuste ConnectionStrings:MaterialId em appsettings.Development.json)
# 2. API — aplica migrations e carrega o catálogo na inicialização
cd backend/src/Mytril.MaterialId.Api && dotnet run --urls http://localhost:5080
# 3. Front — o proxy encaminha /api para a API
cd web && npm install && npx ng serve
```

Abra http://localhost:4200. Enquanto o acesso ao Mytril Identity não está disponível (DEP-01), a tela **Entrar** usa o emissor de tokens de teste `POST /dev/token`, que **só existe em Development**.

## Ambiente de teste (para validação do produto)

A API pode servir o front e simular a IA, para testar o fluxo inteiro sem o servidor de IA e sem o Mytril Identity:

```bash
cd web && npx ng build
cd ../backend/src/Mytril.MaterialId.Api
ASPNETCORE_ENVIRONMENT=Development IaSimulada__Habilitada=true Web__Pasta=../../../web/dist/web/browser \
  dotnet run --urls http://0.0.0.0:8080
```

* **Cadastro e login de teste:** na tela de entrada, "Cadastrar empresa" cria uma empresa de teste (no navegador) e "Entrar" escolhe empresa, nome e perfil (Operador ou Gestor).
* **IA simulada** (`IaSimulada:Habilitada`): cada amarrado enviado recebe, após alguns segundos, um produto real do catálogo, uma quantidade próxima à do amarrado padrão e uma confiança aleatória; cerca de 10% falham, para testar o fluxo de falha. Resultados marcados com a versão `simulador-dev` e faixa "Ambiente de teste" na tela. **Nunca habilitar em produção.**

## Testes

```bash
cd backend && dotnet test      # 62 testes
cd web && npx ng build
```

## Segurança

* JWT do **Mytril Identity**, validado como no Mytril.Audit (emissor `mytril-identity`). Em produção, `Jwt:Key` vem de variável de ambiente.
* Permissões na claim `perm`: `materialid.conferencia.capturar` (operador), `materialid.conferencia.validar` (gestor, dono e diretoria), `materialid.catalogo.gerenciar` (admin da plataforma), `materialid.ia.resultado` (servidor de IA).
* Empresa na claim configurada em `Identity:ClaimEmpresa` (padrão `tenant_id`, **a confirmar no DEP-01**). Filtro global por empresa em todas as consultas e bloqueio de gravação de dados de outra empresa.

## Catálogo

A carga inicial tem **587 itens** das tabelas da **Sampaio** (tubos redondos, quadrados e retangulares, perfis U simples e enrijecido, chapas) e da **Aço Cearense** (tubo retangular). A carga valida a coerência da própria tabela: linhas em que o peso publicado não fecha são marcadas como **"a revisar"** (hoje: Aço Cearense 50×20 esp. 0,90 e Sampaio quadrado 15×15 esp. 0,60).

Para atualizar: salve os HTMLs de produtos da Sampaio numa pasta e rode
`python3 tools/gerar_catalogo_seed.py <pasta> backend/src/Mytril.MaterialId.Catalogo/Infrastructure/Seed/catalogo-seed.json`.
