# ---- build: компилируем оба проекта (кодоген не нужен — артефакты закоммичены, P2 #17) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# egress через прокси нужен только в песочнице/gateway-окружении; в обычном CI арги пусты.
ARG HTTP_PROXY
ARG HTTPS_PROXY
ARG NO_PROXY
ARG PROXY_CA_CERT
ENV HTTP_PROXY=$HTTP_PROXY HTTPS_PROXY=$HTTPS_PROXY NO_PROXY=$NO_PROXY
RUN if [ -n "$PROXY_CA_CERT" ]; then \
      echo "$PROXY_CA_CERT" | base64 -d > /usr/local/share/ca-certificates/sbx-proxy.crt && \
      update-ca-certificates; \
    fi

COPY . .
RUN dotnet publish src/CallCalendar.Api/CallCalendar.Api.csproj -c Release -o /publish/api \
 && dotnet publish src/CallCalendar.Web/CallCalendar.Web.csproj -c Release -o /publish/web

# ---- runtime: один образ, два процесса (entrypoint запускает оба) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish/api ./api
COPY --from=build /publish/web ./web
COPY entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh
EXPOSE 8080
ENTRYPOINT ["./entrypoint.sh"]
