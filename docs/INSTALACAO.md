# Instalação — Clock Tower PC Enhanced

## Código-fonte do launcher

Requer Windows x64 e .NET SDK 10 para compilar. Execute `dotnet publish` conforme o README.

O launcher busca `ClockTowerOriginal_Recompiled.exe` na pasta do próprio launcher, em `game/` e em alguns diretórios relativos compatíveis.

## Instalador completo (quando publicado)

O instalador completo foi concebido para:

1. Perguntar em qual pasta instalar.
2. Pedir a própria imagem `.cue` ou `.iso` de *Clock Tower* PS1; em jogos BIN/CUE, mantenha o BIN correspondente ao lado do CUE.
3. Salvar o caminho da imagem na seção `[disc]` do `game/settings.toml`.
4. Iniciar o launcher único, onde se escolhe idioma e modo gráfico.

**Não redistribua imagens de disco ou arquivos comerciais** junto ao instalador. O funcionamento depende de uma instalação legalmente obtida e compatível. Apenas instalar o launcher não produz automaticamente um jogo recompilado.

Se o instalador completo ainda não estiver anexado à seção Releases, ele **não está disponível para download público**.
