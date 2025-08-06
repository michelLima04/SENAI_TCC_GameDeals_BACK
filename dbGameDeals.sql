
CREATE DATABASE dbgamedeals;
USE dbgamedeals;

CREATE TABLE usuarios (
  id INT NOT NULL AUTO_INCREMENT,
  nome_completo TEXT NOT NULL,
  nome_usuario TEXT NOT NULL,
  email TEXT NOT NULL,
  senha TEXT NOT NULL,
  criado_em DATETIME NOT NULL,
  contribuicoes INT NOT NULL,
  PRIMARY KEY (id)
);

CREATE TABLE promocoes (
  id INT NOT NULL AUTO_INCREMENT,
  url VARCHAR(1000),
  cupom VARCHAR(20),
  site VARCHAR(255) NOT NULL,
  titulo VARCHAR(100) NOT NULL,
  preco DECIMAL(10, 2) NOT NULL,
  imagem_url VARCHAR(1000),
  tempo_postado TIME,
  status_publicacao BOOLEAN NOT NULL,
  motivo_inativacao TEXT,
  usuario_id INT NOT NULL,
  criado_em DATETIME NOT NULL,
  PRIMARY KEY (id),
  FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE comentarios (
  id INT NOT NULL AUTO_INCREMENT,
  is_dono BOOLEAN NOT NULL,
  texto TEXT NOT NULL,
  data DATETIME NOT NULL,
  usuario_id INT,
  promocao_id INT NOT NULL,
  criado_em DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00',
  atualizado_em DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00',
  PRIMARY KEY (id),
  FOREIGN KEY (usuario_id) REFERENCES usuarios(id),
  FOREIGN KEY (promocao_id) REFERENCES promocoes(id) ON DELETE CASCADE
);

CREATE TABLE curtidas (
  id INT NOT NULL AUTO_INCREMENT,
  criado_em DATETIME NOT NULL,
  promocao_id INT NOT NULL,
  usuario_id INT NOT NULL,
  PRIMARY KEY (id),
  FOREIGN KEY (promocao_id) REFERENCES promocoes(id) ON DELETE CASCADE,
  FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE operacoes_log (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_id INT NOT NULL,
  acao VARCHAR(100) NOT NULL,
  entidade_afetada VARCHAR(100),
  entidade_id INT,
  detalhes TEXT,
  criado_em DATETIME NOT NULL,
  PRIMARY KEY (id),
  FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

SELECT * FROM Usuarios;
SELECT * FROM Promocoes;
SELECT * FROM Comentarios;
SELECT * FROM Curtidas;
SELECT * FROM operacoeslog;


