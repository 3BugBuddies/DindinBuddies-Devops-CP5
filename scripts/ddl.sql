/*
  DindinBuddies - DDL das tabelas (Azure SQL Database)

  Referência do esquema do banco. Você NÃO precisa executar este arquivo:
  a API aplica a migration do EF Core (CriacaoInicial) sozinha ao iniciar e cria
  exatamente estas tabelas, índices e restrições.

  Relacionamentos:
    Clientes 1 ── N Contas        (Contas.ClienteId)
    Contas   1 ── N Transacoes    (Transacoes.ContaId: conta da movimentação)
    Contas   1 ── N Transacoes    (Transacoes.ContaDestinoId: destino, só em transferências)

  Nenhuma chave estrangeira apaga em cascata (ON DELETE NO ACTION).
  Datas são gravadas em UTC.
*/

-- ---------------------------------------------------------------------------
-- Clientes
-- ---------------------------------------------------------------------------
CREATE TABLE [Clientes] (
    [Id]             int           NOT NULL IDENTITY,
    [Nome]           nvarchar(150) NOT NULL,
    [Cpf]            char(11)      NOT NULL,                              -- somente dígitos
    [Email]          nvarchar(150) NOT NULL,
    [Telefone]       varchar(20)   NULL,
    [DataNascimento] date          NOT NULL,
    [DataCadastro]   datetime2     NOT NULL DEFAULT (SYSUTCDATETIME()),   -- UTC
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Clientes_Cpf]   ON [Clientes] ([Cpf]);
CREATE UNIQUE INDEX [IX_Clientes_Email] ON [Clientes] ([Email]);

-- ---------------------------------------------------------------------------
-- Contas
-- ---------------------------------------------------------------------------
CREATE TABLE [Contas] (
    [Id]           int           NOT NULL IDENTITY,
    [ClienteId]    int           NOT NULL,
    [Agencia]      char(4)       NOT NULL,                     -- sempre '0001'
    [NumeroConta]  varchar(10)   NOT NULL,                     -- sequencial: 000001, 000002...
    [TipoConta]    tinyint       NOT NULL,                     -- 1 = Corrente, 2 = Poupança
    [Saldo]        decimal(18,2) NOT NULL DEFAULT 0.0,
    [DataAbertura] datetime2     NOT NULL,                     -- UTC
    [Ativa]        bit           NOT NULL DEFAULT CAST(1 AS bit), -- 0 = conta encerrada
    CONSTRAINT [PK_Contas] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Contas_Saldo] CHECK ([Saldo] >= 0),
    CONSTRAINT [FK_Contas_Clientes_ClienteId] FOREIGN KEY ([ClienteId])
        REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Contas_Agencia_NumeroConta] ON [Contas] ([Agencia], [NumeroConta]);
CREATE INDEX [IX_Contas_ClienteId] ON [Contas] ([ClienteId]);

-- ---------------------------------------------------------------------------
-- Transacoes
-- ---------------------------------------------------------------------------
CREATE TABLE [Transacoes] (
    [Id]             bigint        NOT NULL IDENTITY,
    [ContaId]        int           NOT NULL,               -- conta da movimentação (origem, na transferência)
    [Tipo]           tinyint       NOT NULL,               -- 1 = Depósito, 2 = Saque, 3 = Transferência
    [Valor]          decimal(18,2) NOT NULL,
    [DataHora]       datetime2     NOT NULL,               -- UTC
    [Descricao]      nvarchar(200) NULL,
    [ContaDestinoId] int           NULL,                   -- preenchido só em transferências
    CONSTRAINT [PK_Transacoes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Transacoes_ContaDestino] CHECK ([ContaDestinoId] IS NULL OR [ContaDestinoId] <> [ContaId]),
    CONSTRAINT [CK_Transacoes_Valor] CHECK ([Valor] > 0),
    CONSTRAINT [FK_Transacoes_Contas_ContaDestinoId] FOREIGN KEY ([ContaDestinoId])
        REFERENCES [Contas] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transacoes_Contas_ContaId] FOREIGN KEY ([ContaId])
        REFERENCES [Contas] ([Id]) ON DELETE NO ACTION
);

-- Extrato: a conta aparece como origem (ContaId) ou como destino (ContaDestinoId).
CREATE INDEX [IX_Transacoes_ContaId_DataHora]        ON [Transacoes] ([ContaId], [DataHora]);
CREATE INDEX [IX_Transacoes_ContaDestinoId_DataHora] ON [Transacoes] ([ContaDestinoId], [DataHora]);
