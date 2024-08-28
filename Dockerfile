FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.20 AS base

ARG SOFTHSM2_VERSION=2.6.1
ENV SOFTHSM2_VERSION=${SOFTHSM2_VERSION} \
    SOFTHSM2_SOURCES=/tmp/softhsm2 

#Prepare build environment and dependencies.
RUN apk -U upgrade \
 && apk -U add alpine-sdk \
               autoconf \
               automake \
               git \
               libtool \
               openssl-dev \
               opensc

#Build and install SoftHSMv2 from source.
RUN git clone https://github.com/opendnssec/SoftHSMv2.git ${SOFTHSM2_SOURCES}
WORKDIR ${SOFTHSM2_SOURCES}
RUN git checkout ${SOFTHSM2_VERSION} -b ${SOFTHSM2_VERSION} \
    && sh autogen.sh \
    && ./configure --prefix=/opt/softhsm2 \
    && make -j$(($(nproc) + 1)) \
    && make install

#Clean source files after installation.
WORKDIR /root
RUN rm -fr ${SOFTHSM2_SOURCES}

#Define library path.
ENV P11_LIB=/opt/softhsm2/lib/softhsm/libsofthsm2.so \
    PATH=$PATH:/opt/softhsm2/bin 

USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.20 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SoftHSMSigner.csproj", "."]
RUN dotnet restore "./SoftHSMSigner.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./SoftHSMSigner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SoftHSMSigner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SoftHSMSigner.dll"]