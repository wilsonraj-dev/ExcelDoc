Este arquivo explica como Visual Studio criou o projeto.

As seguintes ferramentas foram usadas para gerar este projeto:
- Angular CLI (ng)

As etapas a seguir foram usadas para gerar este projeto:
- Crie um projeto angular com ng: `ng new exceldoc.client --defaults --skip-install --skip-git --no-standalone `.
- Adicione `proxy.conf.js` a chamadas de proxy para o servidor ASP.NET back-end.
- Adicione `aspnetcore-https.js` script para instalar certificados https.
- Atualize `package.json` para chamar `aspnetcore-https.js` e atender com https.
- Atualize `angular.json` para apontar para `proxy.conf.js`.
- Atualize o componente app.component.ts para buscar e exibir informações meteorológicas.
- Modifique app.component.spec.ts com testes atualizados.
- Atualize app.module.ts para importar o HttpClientModule.
- Criar o arquivo de projeto (`exceldoc.client.esproj`).
- Crie `launch.json` para habilitar a depuração.
- Atualize package.json para adicionar `jest-editor-support`.
- Atualize package.json para adicionar `run-script-os`.
- Adicionar `karma.conf.js` para testes de unidade.
- Atualize `angular.json` para apontar para `karma.conf.js`.
- Adicionar projeto à solução.
- Atualize o ponto de extremidade do proxy para ser o ponto de extremidade do servidor back-end.
- Adicione o projeto à lista de projetos de inicialização.
- Grave este arquivo.
