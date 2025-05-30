CREATE DATABASE DbGameDeals;
USE DbGameDeals;

SELECT * FROM Usuarios;
SELECT * FROM Promocoes;
SELECT * FROM Curtidas;
SELECT * FROM Comentarios;

CREATE TABLE Usuarios(
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NomeSobrenome VARCHAR(150) NOT NULL,
    UsuarioNome VARCHAR(20) NOT NULL UNIQUE,
    Email VARCHAR(50) NOT NULL UNIQUE,
    Senha VARCHAR(100) NOT NULL,
    criado_em DATE DEFAULT CURRENT_TIMESTAMP,
    contribuicoes INT DEFAULT 0,
    IsAdmin BOOLEAN NOT NULL DEFAULT FALSE,
    EstaBloqueado BOOLEAN DEFAULT FALSE
);

CREATE TABLE Promocoes(
    id INT AUTO_INCREMENT PRIMARY KEY,
    url VARCHAR(1000),
    cupom VARCHAR(20),
    site VARCHAR(255) NOT NULL,
    titulo VARCHAR(100) NOT NULL,
    preco DECIMAL(10, 2) NOT NULL,
    imagem_url VARCHAR(1000),
    tempo_postado TIME,
    status_publicacao BOOLEAN DEFAULT FALSE,
    motivo_inativacao VARCHAR(500) NULL,
    created_at DATETIME DEFAULT NOW(),
    id_usuario INT,
    FOREIGN KEY (id_usuario) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE TABLE Curtidas(
	id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    id_promocao INT,
    id_usuario INT,
	created_at DATETIME DEFAULT NOW(),
    
    FOREIGN KEY (id_promocao) REFERENCES Promocoes(Id) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE TABLE Comentarios(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	isDono BOOLEAN DEFAULT FALSE, 
    comentario_texto TEXT NOT NULL,
    data_comentario DATETIME NOT NULL,
    id_usuario INT,
    id_promocao INT NOT NULL,	
    FOREIGN KEY (id_usuario) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (id_promocao) REFERENCES Promocoes(id) ON DELETE RESTRICT ON UPDATE CASCADE
);



