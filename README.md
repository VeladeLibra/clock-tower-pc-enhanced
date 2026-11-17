# Clock Tower PC Enhanced — VeladeLibra

**Launcher independente e não oficial para Windows** desenvolvido no projeto Clock Tower PC Enhanced de VeladeLibra. Esta é a **versão 1.8.1 do launcher único**.

> **O jogo original NÃO está incluído.** Este repositório contém o código-fonte do launcher, recursos visuais do projeto e documentação. Não disponibiliza ISO, BIN/CUE, BIOS, arquivos de jogos comerciais ou binários recompilados do jogo.
<img width="315" height="315" alt="WhatsApp Image 2026-11-11 at 10 48 41" src="https://github.com/user-attachments/assets/7ddf2f32-7d88-4bbf-887f-d732ef79d4ee" />
<img width="1292" height="870" alt="WhatsApp Image 2026-11-11 at 10 53 00" src="https://github.com/user-attachments/assets/2deb1667-6e4c-42b0-ab0b-8e2f4e23fc98" />


## Sobre o projeto

Sou fã da franquia *Clock Tower* há mais de 20 anos. A vontade de entender melhor a história em português brasileiro e aproveitar esse clássico motivou o projeto de tradução e, depois, a experiência PC Enhanced.

**Criado por VeladeLibra** · Tradução PT-BR por VeladeLibra.

## O que o launcher V1.8.1 oferece

- Um único launcher com identidade visual sombria e ícone de tesoura, sem abrir outro launcher intermediário.
- Interface com idioma **Português (Brasil)** ou **English**.
- Alternância entre modo clássico e HD 2x; widescreen depende de mod compatível e instalado.
- Preferências salvas automaticamente; sem botão Salvar ou campo exibindo o executável.
- Procura uma instalação local de `ClockTowerOriginal_Recompiled.exe` e inicia o jogo recompilado quando ele está disponível.
- Integração com o mod PT-BR do projeto quando o pacote compatível está instalado; arquivos do mod não fazem parte deste código-fonte.
- Arte da tesoura ajustada na versão 1.8.1 para evitar recorte no cabeçalho.

**Observação:** o mouse, o menu ESC, a tradução e o desempenho do jogo são funcionalidades da instalação e mods do runtime. Este repositório contém apenas o launcher; a presença dessas funcionalidades não é garantida em qualquer compilação do jogo.

## Como compilar o launcher

1. Instale o **.NET SDK 10** no Windows.
2. Abra o PowerShell na pasta do repositório.
3. Execute:

```powershell
dotnet publish .\LauncherUnico\ClockTower_PC_Enhanced.csproj -c Release -r win-x64 --self-contained false -o .\publicado
```

O launcher aparecerá em `publicado\ClockTower_PC_Enhanced.exe`. Para executar, será necessário um runtime .NET Desktop compatível, além dos recursos `.ico` e `.png` publicados junto com o app.

**O executável do jogo recompilado não é gerado por este repositório.** Sua própria instalação compatível deve ter `game\ClockTowerOriginal_Recompiled.exe` ao lado do launcher, ou um local equivalente reconhecido pelo código.

## Instalação e imagem do disco

O projeto possui um instalador Windows mantido separadamente que, nas versões testadas pelo autor, solicita a pasta de instalação e a imagem **ISO/CUE do próprio usuário**. O instalador completo deve ser publicado somente após revisão dos direitos sobre cada arquivo incorporado e teste em computador limpo.

Consulte [Instalação](docs/INSTALACAO.md) e [Notas de lançamento](RELEASE_V181.md).

## Tradução PT-BR

A tradução do jogo possui seu próprio repositório: [clock-tower-ps1-ptbr](https://github.com/VeladeLibra/clock-tower-ps1-ptbr). O launcher PC Enhanced não substitui o instalador da tradução para PS1.

## Problemas, sugestões e créditos

Ao abrir uma Issue, descreva o sistema Windows, o comportamento e o erro, **sem anexar ISO/BIN, BIOS ou arquivos comerciais do jogo**.

**Criado por VeladeLibra.** *Clock Tower* pertence aos seus respectivos titulares. Projeto de fã não oficial, sem afiliação ou endosso.
