# UrbanPulse Curitiba

**UrbanPulse** é uma plataforma de monitoramento de tráfego em tempo real para a cidade de Curitiba. O sistema utiliza uma **Arquitetura Orientada a Eventos (EDA)** e **Persistência Poliglota** para coletar, processar e analisar dados de fluxo viário.

---

## 🚀 Tecnologias Utilizadas

* **Linguagem:** C# (.NET 10)
* **Mensageria:** RabbitMQ (Processamento assíncrono)
* **Banco de Dados (Documentos):** MongoDB (Fonte da verdade / Histórico)
* **Motor de Busca e Analytics:** Elasticsearch 8.19+ (Buscas geo-espaciais e agregações)
* **API de Dados:** TomTom Traffic API

---

## 🏗️ Arquitetura do Sistema

O projeto é dividido em quatro componentes principais para garantir escalabilidade:

1.  **UrbanPulse.Producer:** Um worker service que consome dados da API TomTom e publica eventos no RabbitMQ.
2.  **UrbanPulse.Shared:** Biblioteca de contratos e modelos de dados comuns entre os serviços.
3.  **UrbanPulse.Consumer:** Serviço que processa as filas do RabbitMQ e realiza a escrita dupla (Dual Write) no MongoDB e Elasticsearch.
4.  **UrbanPulse.API:** Interface REST que expõe os dados processados para o usuário final.

---

## ✨ Funcionalidades em Destaque

### 🔍 Busca por Proximidade (Geofencing)
Diferente de bancos tradicionais, utilizamos o **Geo-Mapping** do Elasticsearch para permitir buscas num raio específico:
* *Exemplo:* "Encontrar incidentes num raio de 2km da Praça Tiradentes".

### 📊 Analytics em Tempo Real
Implementação de **Agregações Complexas** para calcular o nível de congestionamento médio por bairro (Batel, Centro, Linha Verde), permitindo uma visão macro da cidade em milissegundos.

### ⚡ Performance .NET 10
Utilização da sintaxe de inicialização de objetos e processamento assíncrono de ponta a ponta para garantir baixa latência e alta disponibilidade.

---

## 🛠️ Como Executar

1.  **Pré-requisitos:**
    * Docker e Docker Compose.
    * SDK do .NET 10.
    * Chave de API da TomTom.

2.  **Configuração do Ambiente:**
    ```bash
    # Subir os serviços de infraestrutura
    docker-compose up -d
    ```

3.  **Execução:**
    ```bash
    # Na pasta src/
    dotnet run --project UrbanPulse.Producer
    dotnet run --project UrbanPulse.Consumer
    dotnet run --project UrbanPulse.API
    ```

---

## 📡 Endpoints Principais (API)

| Método | Endpoint | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/events` | Retorna os últimos 50 incidentes registrados. |
| `GET` | `/api/events/proximity` | Busca eventos por coordenadas (Lat/Lon) e Raio. |
| `GET` | `/api/events/stats` | Agregação de média de congestionamento por bairro. |
