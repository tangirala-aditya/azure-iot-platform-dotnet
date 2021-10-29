// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import { HttpClient } from "utilities/httpClient";
import { toUserTenantModel } from "./models";
import { map } from "rxjs/operators";

const ENDPOINT = Config.serviceUrls.identityGateway;

/** Contains methods for calling the Identity Gateway service */
export class IdentityGatewayService {
    /** Returns a list of users */
    static getUsers() {
        return HttpClient.get(`${ENDPOINT}tenants/users`).pipe(
            map((res = { Models: [] }) => res.Models),
            map(toUserTenantModel)
        );
    }

    /** Returns all the users who are not currently super users */
    static getAllNonSystemAdmins() {
        return HttpClient.get(
            `${ENDPOINT}systemAdmin/getAllNonSystemAdmins`
        ).pipe(map((res = { Models: [] }) => res.Models));
    }

    /** Retuns all the current super users in the system */
    static getAllSystemAdmins() {
        return HttpClient.get(`${ENDPOINT}systemAdmin/getAllSystemAdmins`).pipe(
            map((res = { Models: [] }) => res.Models)
        );
    }

    /** Delete a User */
    static deleteUser(id) {
        return HttpClient.delete(`${ENDPOINT}tenants/${id}`).pipe(
            map((t) => id)
        );
    }

    static deleteSystemAdmin(id) {
        return HttpClient.delete(`${ENDPOINT}systemAdmin/${id}`).pipe(
            map((t) => id)
        );
    }

    static addSystemAdmin(userId, name) {
        return HttpClient.post(`${ENDPOINT}systemAdmin`, {
            userId: userId,
            name: name,
        });
    }

    /** Invite a new User */
    static invite(email, role) {
        return HttpClient.post(`${ENDPOINT}tenants/invite`, {
            email_address: email,
            role: role,
        }).pipe(map((t) => toUserTenantModel([t])));
    }

    /** Add a new Service Principal */
    static addSP(appid, role) {
        return HttpClient.post(`${ENDPOINT}tenants/${appid}`, {
            PartitionKey: "", // placeholder not used
            RowKey: "", // placeholder not used
            Roles: `['${role}']`,
            Type: "Client Credentials",
            Name: appid,
        }).pipe(map((t) => toUserTenantModel([t])));
    }

    static getUserActiveDeviceGroup() {
        return HttpClient.get(`${ENDPOINT}settings/ActiveDeviceGroup`).pipe(
            map((setting) => setting && setting.value)
        );
    }

    static updateUserActiveDeviceGroup(value) {
        return HttpClient.put(
            `${ENDPOINT}settings/ActiveDeviceGroup/${value}`
        ).pipe(map((setting) => setting && setting.value));
    }

    static VerifyAndRefreshCache() {
        HttpClient.get(`${ENDPOINT}settings/LatestBuildNumber`)
            .pipe(map((setting) => setting && setting.value))
            .subscribe((value) => {
                if (
                    HttpClient.getLocalStorageValue("latestBuildNumber") !==
                    value
                ) {
                    window.location.reload(true);
                    HttpClient.setLocalStorageValue("latestBuildNumber", value);
                }
            });
    }

    /* Method that returns the mode of the dashboard*/
    static getDashboardMode() {
        return HttpClient.get(`${ENDPOINT}settings/DashboardMode`).pipe(
            map((setting) => setting && setting.value)
        );
    }
}
