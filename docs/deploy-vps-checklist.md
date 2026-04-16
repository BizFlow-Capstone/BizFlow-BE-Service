# BizFlow Deploy Checklist (BE + AI + Docker Hub + VPS)

Muc tieu:
- Build image tren may local
- Push len Docker Hub
- Pull va chay tren VPS
- Verify nhanh sau deploy
- Co rollback khi can

## 0) Bien can thay truoc khi chay
- DOCKERHUB_USER: username Docker Hub cua ban (vi du: thienlm30)
- TAG: version deploy (vi du: 2026-04-14.1)
- VPS_DEPLOY_DIR: thu muc chua docker compose tren VPS (vi du: /opt/bizflow/deployment)
- NPM_CONTAINER: ten container Nginx Proxy Manager (vi du: nginx-proxy-manager-app-1)

## 1) Build va push image tu may local (Windows)
Chay tai thu muc BizFlow-BE-Service.

1. Build BE image

    docker build -t DOCKERHUB_USER/bizflow-api:TAG -f .\bizflow-platform\Dockerfile .\bizflow-platform
    docker tag DOCKERHUB_USER/bizflow-api:TAG DOCKERHUB_USER/bizflow-api:latest

2. Build AI image

    docker build -t DOCKERHUB_USER/bizflow-ai:TAG -f ..\BizFlow-AI-Service\Dockerfile ..\BizFlow-AI-Service
    docker tag DOCKERHUB_USER/bizflow-ai:TAG DOCKERHUB_USER/bizflow-ai:latest

3. Push len Docker Hub

    docker push DOCKERHUB_USER/bizflow-api:TAG
    docker push DOCKERHUB_USER/bizflow-api:latest
    docker push DOCKERHUB_USER/bizflow-ai:TAG
    docker push DOCKERHUB_USER/bizflow-ai:latest

4. Kiem tra tren Docker Hub
- Co 2 repo: bizflow-api, bizflow-ai
- Moi repo co tag TAG va latest

## 2) Chinh compose tren VPS de dung image (khong build tren VPS)
Khuyen nghi tao 1 file override rieng tren VPS, vi du docker-compose.vps.yml.

1. Tao shared network de NPM co the goi API bang ten container:

        docker network create bizflow-public
        docker network connect bizflow-public NPM_CONTAINER

2. Tao file docker-compose.vps.yml trong thu muc deploy (copy theo mau duoi):

        services:
            nginx:
                profiles: ["disabled"]

            bizflow-api:
                image: ${DOCKERHUB_USER}/bizflow-api:${IMAGE_TAG}
                build: null
                restart: unless-stopped
                expose:
                    - "8080"
                networks:
                    - bizflow-network
                    - bizflow-public

            bizflow-ai:
                image: ${DOCKERHUB_USER}/bizflow-ai:${IMAGE_TAG}
                build: null
                restart: unless-stopped

        networks:
            bizflow-public:
                external: true

3. Them vao .env.prod:

        DOCKERHUB_USER=thienlm30
        IMAGE_TAG=2026-04-14.1

Noi dung toi thieu can co:
- bizflow-api dung image DOCKERHUB_USER/bizflow-api:TAG
- bizflow-ai dung image DOCKERHUB_USER/bizflow-ai:TAG
- Xoa/bo qua phan build
- restart: unless-stopped

Luu y quan trong:
- Neu ban dang dung Nginx Proxy Manager rieng, KHONG chay them service nginx bind 80/443 trong compose cua BE de tranh trung cong.
- AI nen internal only (khong map cong ra ngoai).

## 3) Deploy tren VPS
SSH vao VPS, den thu muc deploy.

1. Pull image moi

    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod --profile ai pull

2. Restart service voi image moi

    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod --profile ai up -d

3. Xem trang thai

    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod ps

4. Xem log nhanh

    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod logs -f --tail=100 bizflow-api bizflow-ai

## 4) Verify sau deploy (5-10 phut)
1. DNS
- api.bizflow.asia da tro dung IP VPS

2. SSL
- Truy cap https://api.bizflow.asia
- Cert hop le (khong bao loi browser)

3. API health
- Goi endpoint health/check cua BE
- Goi 1 endpoint co su dung AI de test luong BE -> AI

4. Security quick check
- Khong public cong AI (5000/8000)
- Khong public Redis/RabbitMQ management neu khong can
- Firewall chi mo 22, 80, 443

## 5) Rollback nhanh
Neu TAG moi loi, rollback ve TAG cu:

1. Sua TAG cu trong docker-compose.vps.yml (api + ai)
2. Chay lai:

    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod --profile ai pull
    docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.vps.yml --env-file .env.prod --profile ai up -d

3. Verify lai API va log

## 6) Quy trinh redeploy chuan (lan sau)
1. Tang TAG moi
2. Build + push 2 image
3. SSH VPS, pull
4. up -d
5. Kiem tra health + logs
6. Xong
