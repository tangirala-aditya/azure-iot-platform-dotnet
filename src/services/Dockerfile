ARG DOTNET_CORE_SDK_VERSION=3.1
FROM mcr.microsoft.com/dotnet/core/sdk:${DOTNET_CORE_SDK_VERSION}
RUN adduser --disabled-password --gecos '' nginx
ARG DOTNET_CORE_SDK_VERSION
ENV DOTNET_CORE_SDK_VERSION ${DOTNET_CORE_SDK_VERSION}
EXPOSE 80
ENV ASPNETCORE_URLS http://+:80/
WORKDIR /azure-iot-services-dotnet/
RUN chown -R nginx /azure-iot-services-dotnet/ /usr/share/dotnet/**/*
COPY *.props .
COPY *.json .
COPY *.ruleset .
ARG SERVICE_NAME
ENV SERVICE_NAME ${SERVICE_NAME}
WORKDIR /azure-iot-services-dotnet/src/services/
COPY src/services/*.props .
COPY src/services/common/Services/*.csproj common/Services/
COPY src/services/${SERVICE_NAME}/Services/*.csproj ${SERVICE_NAME}/Services/
COPY src/services/${SERVICE_NAME}/WebService/*.csproj ${SERVICE_NAME}/WebService/
RUN chown -R nginx /azure-iot-services-dotnet/src/services/**/*
RUN chmod -R 777 /azure-iot-services-dotnet/src/services/**/*
RUN dotnet restore ${SERVICE_NAME}/WebService/*.csproj
COPY src/services/common common/
COPY src/services/${SERVICE_NAME} ${SERVICE_NAME}/
WORKDIR /azure-iot-services-dotnet/src/services/${SERVICE_NAME}/WebService
ARG BUILD_CONFIGURATION=Debug
ENV BUILD_CONFIGURATION ${BUILD_CONFIGURATION}
ARG APP_CONFIGURATION_CONNECTION_STRING
ENV AppConfigurationConnectionString ${APP_CONFIGURATION_CONNECTION_STRING}
RUN chown -R nginx /azure-iot-services-dotnet/src/services/**/*/  /usr/share/dotnet/**/* /tmp/**/*/
USER nginx
RUN dotnet build  --configuration ${BUILD_CONFIGURATION} WebService.csproj
ARG WEB_SERVICE_ASSEMBLY_NAME
ENV WEB_SERVICE_ASSEMBLY_NAME ${WEB_SERVICE_ASSEMBLY_NAME}
ENTRYPOINT dotnet bin/${BUILD_CONFIGURATION}/netcoreapp${DOTNET_CORE_SDK_VERSION}/${WEB_SERVICE_ASSEMBLY_NAME}
