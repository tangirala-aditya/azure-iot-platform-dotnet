FROM node:14-alpine as codebuilder
WORKDIR /app
RUN apk add --no-cache bash
RUN apk --update add python3
RUN apk add --no-cache build-base
RUN apk add --no-cache curl
RUN apk add --no-cache jq
RUN adduser -u 1010 --disabled-password nginx && chown -R nginx /app/

RUN apk update
RUN apk add bash py3-pip
RUN apk add --virtual=build gcc libffi-dev musl-dev openssl-dev python3-dev make
RUN pip3 --no-cache-dir install -U pip
RUN pip3 --no-cache-dir install azure-cli
RUN apk del --purge build


MAINTAINER Andrew Schmidt (https://github.com/andrewschmidt-a)

LABEL Tags="Azure,IoT,Solutions,React,SPA"

RUN echo "Installing web server..." \
 && apk add --no-cache nginx \
 && mkdir -p /var/lib/nginx /var/cache/nginx /var/tmp/nginx /var/log/nginx \
 && echo "Removing unused files..." \
 && apk del --force --purge alpine-keys apk-tools \
 && rm -rf /var/cache/apk /etc/apk /lib/apk

EXPOSE 10080

COPY package.json .
COPY azure-iot-ux-fluent-controls/package.json azure-iot-ux-fluent-controls/package.json
COPY azure-iot-ux-fluent-css/package.json azure-iot-ux-fluent-css/package.json
RUN echo "Installing node packages ..." && npm --add-python-to-path='true' install
COPY ./ /app
RUN ["chmod", "+x", "/app/run.sh"]
RUN ["chmod", "+x", "/app/build.sh"]
RUN ["chmod", "+x", "/app/set_env.sh"]
RUN ["chmod", "+x", "/app/set_policies.sh"]
RUN chown -R nginx /var/lib/nginx/ /var/log/nginx/ /var/cache/nginx/ /var/tmp/nginx/ /app/**/*
#RUN chmod +rw /app/logs /etc/nginx/ /var/log/nginx/ /var/cache/nginx/ /app/
USER nginx
RUN mkdir /app/logs
VOLUME /app/logs

ENTRYPOINT ["/bin/bash", "-c", "/app/run.sh"]
