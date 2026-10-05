/*
  DindinBuddies - consultas para conferir a persistência no banco

  Use depois de cada operação do CRUD (pelo front, pelo Swagger ou pelo Postman)
  para mostrar o resultado gravado em cada tabela.

  Na Azure: portal > SQL databases > DindinBuddies > Query editor (preview),
  entrando com o usuário dindinadmin. Se o Query editor pedir, libere o seu IP
  no firewall do servidor (ele oferece um botão para isso).
  Localmente: localhost,1433, usuário sa.
*/

-- 1) Clientes ----------------------------------------------------------------
SELECT Id, Nome, Cpf, Email, Telefone, DataNascimento, DataCadastro
FROM Clientes
ORDER BY Id;

-- 2) Contas, com o nome do cliente -------------------------------------------
SELECT c.Id, c.Agencia, c.NumeroConta,
       CASE c.TipoConta WHEN 1 THEN 'Corrente' WHEN 2 THEN 'Poupança' END AS TipoConta,
       c.Saldo,
       CASE c.Ativa WHEN 1 THEN 'Ativa' ELSE 'Encerrada' END AS Situacao,
       c.ClienteId, cl.Nome AS Cliente, c.DataAbertura
FROM Contas c
JOIN Clientes cl ON cl.Id = c.ClienteId
ORDER BY c.Id;

-- 3) Transações, com as contas de origem e destino ----------------------------
SELECT t.Id,
       CASE t.Tipo WHEN 1 THEN 'Depósito' WHEN 2 THEN 'Saque' WHEN 3 THEN 'Transferência' END AS Tipo,
       t.Valor, t.Descricao, t.DataHora,
       o.NumeroConta AS ContaOrigem,
       d.NumeroConta AS ContaDestino
FROM Transacoes t
JOIN Contas o      ON o.Id = t.ContaId
LEFT JOIN Contas d ON d.Id = t.ContaDestinoId
ORDER BY t.Id;

-- 4) Conferência: saldo de cada conta x soma das movimentações ----------------
-- (as duas colunas devem ser iguais; mostra que o saldo gravado é consistente)
SELECT c.Id, c.NumeroConta, c.Saldo AS SaldoGravado,
       ISNULL(SUM(CASE
                    WHEN t.Tipo = 1 AND t.ContaId = c.Id        THEN  t.Valor
                    WHEN t.Tipo = 2 AND t.ContaId = c.Id        THEN -t.Valor
                    WHEN t.Tipo = 3 AND t.ContaId = c.Id        THEN -t.Valor
                    WHEN t.Tipo = 3 AND t.ContaDestinoId = c.Id THEN  t.Valor
                  END), 0) AS SaldoCalculado
FROM Contas c
LEFT JOIN Transacoes t ON t.ContaId = c.Id OR t.ContaDestinoId = c.Id
GROUP BY c.Id, c.NumeroConta, c.Saldo
ORDER BY c.Id;
