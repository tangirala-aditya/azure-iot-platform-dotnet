import Config from "app.config";
import { map } from "rxjs/operators";
import { HttpClient } from "utilities/httpClient";
import { toTenantModel, toAlertingStatusModel } from "./models";

const TENANT_MANAGER_ENDPOINT = Config.serviceUrls.tenantManager;

export class TenantService {
    /** Get all tenants for a user */
    static getAllTenants() {
        return HttpClient.get(`${TENANT_MANAGER_ENDPOINT}tenant/all`).pipe(
            map(toTenantModel)
        );
    }

    /** Creates a new tenant */
    static createTenant() {
        return HttpClient.post(`${TENANT_MANAGER_ENDPOINT}tenant`);
    }

    /** Delete a tenant */
    static deleteTenant() {
        return HttpClient.delete(`${TENANT_MANAGER_ENDPOINT}tenant`);
    }

    /** Returns whether a tenant is ready or not */
    static tenantIsDeployed(tenantId) {
        return HttpClient.get(
            `${TENANT_MANAGER_ENDPOINT}tenantready/${tenantId}`
        );
    }

    /** Returns the display value for the tenantGuid */
    static processDisplayValue(tenantGuid) {
        // TODO: Add tenant name setting here in place of this generic value ~ Joe Bethke
        return `tenant#${tenantGuid.substring(0, 5)}`;
    }

    /** Returns the status of the alerting feature */
    static getAlertingStatus(createIfNotExists = false) {
        return HttpClient.get(
            `${TENANT_MANAGER_ENDPOINT}alerting?createIfNotExists=${createIfNotExists}`
        ).pipe(map(toAlertingStatusModel));
    }

    /** Enables the alerting feature */
    static alertingEnable() {
        return HttpClient.post(`${TENANT_MANAGER_ENDPOINT}alerting`).pipe(
            map(toAlertingStatusModel)
        );
    }

    /** Disables the alerting feature */
    static alertingDisable() {
        return HttpClient.delete(`${TENANT_MANAGER_ENDPOINT}alerting`).pipe(
            map(toAlertingStatusModel)
        );
    }

    /** Starts the alerting feature */
    static alertingStart() {
        return HttpClient.post(`${TENANT_MANAGER_ENDPOINT}alerting/start`).pipe(
            map(toAlertingStatusModel)
        );
    }

    /** Starts the alerting feature */
    static alertingStop() {
        return HttpClient.post(`${TENANT_MANAGER_ENDPOINT}alerting/stop`).pipe(
            map(toAlertingStatusModel)
        );
    }

    /** Creates a new tenant */
    static updateTenant(tenantId, tenantName) {
        return HttpClient.put(
            `${TENANT_MANAGER_ENDPOINT}tenant/${tenantId}`,
            `"${tenantName}"`
        );
    }

    /** Returns the configured grafana url of a tenant */
    static getGrafanaUrl() {
        return HttpClient.get(
            `${TENANT_MANAGER_ENDPOINT}tenant/GrafanaUrl`
        ).pipe(map((value) => value));
    }

    /** Returns the configured grafana orgid of a tenant */
    static getGrafanaOrgId() {
        return HttpClient.get(
            `${TENANT_MANAGER_ENDPOINT}tenant/GrafanaOrgId`
        ).pipe(map((value) => value));
    }
}
