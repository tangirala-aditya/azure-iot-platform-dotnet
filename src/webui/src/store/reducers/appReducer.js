// Copyright (c) Microsoft. All rights reserved.

import "rxjs";
import { EMPTY, of, interval, scheduled } from "rxjs";
import dot from "dot-object";
import moment from "moment";
import { schema, normalize } from "normalizr";
import { createSelector } from "reselect";
import update from "immutability-helper";
import { ofType } from "redux-observable";
import {
    map,
    distinctUntilChanged,
    switchMap,
    catchError,
    mergeMap,
    first,
    filter,
    take,
    mergeAll,
    delay,
} from "rxjs/operators";

import Config from "app.config";
import {
    AuthService,
    ConfigService,
    GitHubService,
    DiagnosticsService,
    TelemetryService,
    TenantService,
    IdentityGatewayService,
} from "services";
import {
    createAction,
    createReducerScenario,
    createEpicScenario,
    errorPendingInitialState,
    pendingReducer,
    errorReducer,
    setPending,
    toActionCreator,
    getPending,
    getError,
} from "store/utilities";
import { svgs, compareByProperty } from "utilities";
import {
    toSinglePropertyDiagnosticsModel,
    SystemDefaultMapping,
} from "services/models";
import { HttpClient } from "utilities/httpClient";

// ========================= Epics - START
const handleError = (fromAction) => (error) =>
    of(redux.actions.registerError(fromAction.type, { error, fromAction }));

export const epics = createEpicScenario({
    /** Initializes the redux state */
    initializeApp: {
        type: "APP_INITIALIZE",
        epic: () => [
            epics.actions.fetchUser(),
            epics.actions.fetchColumnMappings(),
            epics.actions.fetchColumnOptions(),
            epics.actions.fetchDeviceGroups(),
            epics.actions.fetchLogo(),
            epics.actions.fetchReleaseInformation(),
            epics.actions.fetchSolutionSettings(),
            epics.actions.fetchTelemetryStatus(),
            epics.actions.fetchAlerting(),
            epics.actions.fetchGrafanaUrl(),
            epics.actions.fetchGrafanaOrgId(),
        ],
    },

    /** Log diagnostics data */
    logEvent: {
        type: "APP_LOG_EVENT",
        epic: ({ payload }, store) => {
            const diagnosticsOptIn = getDiagnosticsOptIn(store.value);
            if (diagnosticsOptIn) {
                payload.sessionId = getSessionId(store.value);
                payload.eventProperties.CurrentWindow = getCurrentWindow(
                    store.value
                );
                return DiagnosticsService.logEvent(payload).pipe(
                    /* We don't want anymore action to be executed after this call
              and hence return empty observable in flatMap */
                    mergeMap((_) => EMPTY),
                    catchError((_) => EMPTY)
                );
            }
            return EMPTY;
        },
    },

    /** Get the user */
    fetchUser: {
        type: "APP_USER_FETCH",
        epic: (fromAction, store) =>
            AuthService.getCurrentUser().pipe(
                map(toActionCreator(redux.actions.updateUser, fromAction)),
                catchError(handleError(fromAction))
            ),
    },

    /** Get the Alerting Status */
    fetchAlerting: {
        type: "APP_ALERTING_FETCH",
        epic: (fromAction, store) =>
            TenantService.getAlertingStatus(true).pipe(
                map(toActionCreator(redux.actions.updateAlerting, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Get solution settings */
    fetchSolutionSettings: {
        type: "APP_FETCH_SOLUTION_SETTINGS",
        epic: (fromAction) =>
            ConfigService.getSolutionSettings().pipe(
                map(
                    toActionCreator(
                        redux.actions.updateSolutionSettings,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    /** Get Telemetry Status */
    fetchTelemetryStatus: {
        type: "APP_FETCH_TELEMETRY_STATUS",
        epic: (fromAction) =>
            TelemetryService.getStatus().pipe(
                map(
                    toActionCreator(
                        redux.actions.updateTelemetryProperties,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    /** Update solution settings */
    updateDiagnosticsOptIn: {
        type: "APP_UPDATE_DIAGNOSTICS_OPTOUT",
        epic: (fromAction, store) => {
            const currSettings = getSettings(store.value),
                settings = {
                    name: currSettings.name,
                    description: currSettings.description,
                    diagnosticsOptIn: fromAction.payload,
                };

            let isDiagnosticOptIn = fromAction.payload ? "true" : "false",
                logPayload = toSinglePropertyDiagnosticsModel(
                    "Settings_DiagnosticsToggle",
                    "isEnabled",
                    isDiagnosticOptIn
                );
            logPayload.sessionId = getSessionId(store.value);
            logPayload.eventProperties.CurrentWindow = getCurrentWindow(
                store.value
            );
            DiagnosticsService.logEvent(logPayload).subscribe();

            return ConfigService.updateSolutionSettings(settings).pipe(
                map(
                    toActionCreator(
                        redux.actions.updateSolutionSettings,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            );
        },
    },

    /** Get the account's device groups */
    fetchDeviceGroups: {
        type: "APP_DEVICE_GROUPS_FETCH",
        epic: (fromAction, store) =>
            ConfigService.getDeviceGroups().pipe(
                mergeMap((payload) => {
                    const deviceGroups = payload.sort(
                            compareByProperty("displayName", true)
                        ),
                        actions = [];
                    actions.push(
                        toActionCreator(
                            redux.actions.updateDeviceGroups,
                            fromAction
                        )(deviceGroups)
                    );
                    actions.push(epics.actions.fetchSelectedDeviceGroup());
                    return actions;
                }),
                catchError(handleError(fromAction))
            ),
    },

    fetchColumnMappings: {
        type: "APP_COLUMN_MAPPINGS_GETCH",
        epic: (fromAction, store) =>
            ConfigService.getColumnMappings().pipe(
                map(
                    toActionCreator(
                        redux.actions.updateColumnMappings,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    fetchColumnOptions: {
        type: "APP_COLUMN_OPTIONS_FETCH",
        epic: (fromAction, store) =>
            ConfigService.getColumnOptions().pipe(
                map(
                    toActionCreator(
                        redux.actions.updateColumnOptions,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    fetchSelectedDeviceGroup: {
        type: "APP_SELECTED_DEVICE_GROUP_FETCH",
        epic: (fromAction, store) =>
            IdentityGatewayService.getUserActiveDeviceGroup().pipe(
                map(
                    (value) =>
                        value ||
                        Object.keys(getDeviceGroupEntities(store.value))[0]
                ),
                map(
                    toActionCreator(
                        redux.actions.updateActiveDeviceGroup,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    updateActiveDeviceGroup: {
        type: "APP_ACTIVE_DEVICE_GROUP_UPDATE",
        epic: (fromAction) =>
            IdentityGatewayService.updateUserActiveDeviceGroup(
                fromAction.payload
            ).pipe(
                map(
                    toActionCreator(
                        redux.actions.updateActiveDeviceGroup,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },
    /** Listen to route events and emit a route change event when the url changes */
    detectRouteChange: {
        type: "APP_ROUTE_EVENT",
        rawEpic: (action$, store, actionType) =>
            action$.pipe(
                ofType(actionType),
                map(({ payload }) => payload), // payload === pathname
                distinctUntilChanged(),
                map(createAction("EPIC_APP_ROUTE_CHANGE"))
            ),
    },

    /** Get the logo and company name from the config service */
    fetchLogo: {
        type: "APP_FETCH_LOGO",
        epic: (fromAction) =>
            ConfigService.getLogo().pipe(
                map(toActionCreator(redux.actions.updateLogo, fromAction)),
                catchError(handleError(fromAction))
            ),
    },

    /** Set the logo and/or company name in the config service */
    updateLogo: {
        type: "APP_UPDATE_LOGO",
        epic: (fromAction) =>
            ConfigService.setLogo(
                fromAction.payload.logo,
                fromAction.payload.headers
            ).pipe(
                map(toActionCreator(redux.actions.updateLogo, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Update alerting */
    updateAlerting: {
        type: "APP_UPDATE_ALERTING",
        epic: (fromAction) =>
            of(fromAction.payload).pipe(
                map(toActionCreator(redux.actions.updateAlerting, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Get the current release version and release notes link from GitHub */
    fetchReleaseInformation: {
        type: "APP_FETCH_RELEASE_INFO",
        epic: (fromAction) =>
            GitHubService.getReleaseInfo().pipe(
                map(
                    toActionCreator(
                        redux.actions.getReleaseInformation,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    fetchGrafanaUrl: {
        type: "APP_FETCH_GRAFANA_URL",
        epic: (fromAction) =>
            TenantService.getGrafanaUrl().pipe(
                map(toActionCreator(redux.actions.getGrafanaUrl, fromAction)),
                catchError(handleError(fromAction))
            ),
    },

    fetchGrafanaOrgId: {
        type: "APP_FETCH_GRAFANA_ORG",
        epic: (fromAction) =>
            TenantService.getGrafanaOrgId().pipe(
                map(toActionCreator(redux.actions.getGrafanaOrgId, fromAction)),
                catchError(handleError(fromAction))
            ),
    },

    /** Get solution's action settings */
    fetchActionSettings: {
        type: "APP_FETCH_SOLUTION_ACTION_SETTINGS",
        epic: (fromAction) =>
            ConfigService.getActionSettings().pipe(
                map(
                    toActionCreator(
                        redux.actions.updateActionSettings,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    /** Poll the server for the action settings. */
    pollActionSettings: {
        type: "APP_POLL_SOLUTION_ACTION_SETTINGS",
        rawEpic: (action$, store, actionType) =>
            action$.pipe(
                ofType(actionType),
                switchMap((fromAction) => {
                    const poll$ = interval(
                            Config.actionSetupPollingInterval
                        ).pipe(
                            switchMap((_) => ConfigService.getActionSettings()),
                            map(
                                toActionCreator(
                                    redux.actions.updateActionSettings,
                                    fromAction
                                )
                            ),
                            filter((updateAction) => {
                                const {
                                        entities: { actionSettings },
                                    } = normalize(
                                        updateAction.payload,
                                        actionSettingsListSchema
                                    ),
                                    isEnabled = dot.pick(
                                        "Email.isEnabled",
                                        actionSettings
                                    );
                                return isEnabled;
                            }),
                            take(1),
                            catchError(handleError(fromAction))
                        ),
                        timeout$ = of(
                            redux.actions.updateActionPollingTimeout()
                        ).pipe(delay(Config.actionSetupPollingTimeLimit));
                    return scheduled(poll$, timeout$).pipe(mergeAll(), first());
                })
            ),
    },
});
// ========================= Epics - END

// ========================= Schemas - START
const deviceGroupSchema = new schema.Entity("deviceGroups"),
    deviceGroupListSchema = new schema.Array(deviceGroupSchema),
    actionSettingsSchema = new schema.Entity("actionSettings"),
    actionSettingsListSchema = new schema.Array(actionSettingsSchema),
    columnMappingSchema = new schema.Entity("columnMappings"),
    columnMappingListSchema = new schema.Array(columnMappingSchema),
    columnOptionsSchema = new schema.Entity("columnOptions"),
    columnOptionsListSchema = new schema.Array(columnOptionsSchema),
    // ========================= Schemas - END

    // ========================= Reducers - START
    initialState = {
        ...errorPendingInitialState,
        deviceGroups: {},
        deviceGroupFilters: {},
        activeDeviceQueryConditions: [],
        activeDeviceGroupId: undefined,
        theme: "mmm",
        version: undefined,
        releaseNotesUrl: undefined,
        grafanaUrl: undefined,
        grafanaOrgId: undefined,
        timeSeriesExplorerUrl: undefined,
        logo: svgs.mmmLogo,
        name: "header.companyName",
        isDefaultLogo: true,
        deviceGroupFlyoutIsOpen: false,
        createDeviceQueryFlyoutIsOpen: false,
        timeInterval: "PT1H",
        settings: {
            azureMapsKey: "",
            description: "",
            name: "",
            diagnosticsOptIn: true,
        },
        user: {
            email: "",
            roles: new Set(),
            permissions: new Set(),
        },
        actionSettings: undefined,
        applicationPermissionsAssigned: undefined,
        actionPollingTimeout: undefined,
        sessionId: moment().utc().unix(),
        currentWindow: "",
        alerting: {
            jobState: "Not Enabled",
            isActive: false,
        },
        columnMappings: [],
        columnOptions: [],
    },
    updateUserReducer = (state, { payload, fromAction }) => {
        return update(state, {
            user: {
                email: { $set: payload.email },
                roles: { $set: new Set(payload.roles) },
                permissions: { $set: new Set(payload.permissions) },
                availableTenants: { $set: new Set(payload.availableTenants) },
                tenant: { $set: payload.tenant },
                token: { $set: payload.token },
                isSystemAdmin: { $set: payload.isSystemAdmin },
                id: { $set: payload.id },
            },
            ...setPending(fromAction.type, false),
        });
    },
    updateAlertingReducer = (state, { payload, fromAction }) => {
        return update(state, {
            alerting: {
                jobState: { $set: payload.jobState },
                isActive: { $set: payload.isActive },
            },
            ...setPending(fromAction.type, false),
        });
    },
    updateTelemetryPropertiesReducer = (state, { payload, fromAction }) => {
        return update(state, {
            timeSeriesExplorerUrl: { $set: payload.properties.tsiExplorerUrl },
            ...setPending(fromAction.type, false),
        });
    },
    updateDeviceGroupsReducer = (state, { payload, fromAction }) => {
        const {
            entities: { deviceGroups },
        } = normalize(payload, deviceGroupListSchema);
        return update(state, {
            deviceGroups: { $set: deviceGroups },
            ...setPending(fromAction.type, false),
        });
    },
    deleteDeviceGroupsReducer = (state, { payload }) =>
        update(state, {
            deviceGroups: { $unset: [...payload] },
        }),
    insertDeviceGroupsReducer = (state, { payload }) => {
        const {
            entities: { deviceGroups },
        } = normalize(payload, deviceGroupListSchema);
        return update(state, {
            deviceGroups: { $merge: deviceGroups },
        });
    },
    updateColumnMappingsReducer = (state, { payload, fromAction }) => {
        if (!payload.find((p) => p.id === "Default")) {
            payload.push(SystemDefaultMapping);
        }
        const {
            entities: { columnMappings },
        } = normalize(payload, columnMappingListSchema);
        return update(state, {
            columnMappings: { $set: columnMappings },
            ...setPending(fromAction.type, false),
        });
    },
    deleteColumnMappingsReducer = (state, { payload }) =>
        update(state, {
            columnMappings: { $unset: [...payload] },
        }),
    updateColumnOptionsReducer = (state, { payload, fromAction }) => {
        const {
            entities: { columnOptions },
        } = normalize(payload, columnOptionsListSchema);
        return update(state, {
            columnOptions: { $set: columnOptions },
            ...setPending(fromAction.type, false),
        });
    },
    insertColumnOptionsReducer = (state, { payload }) => {
        const {
            entities: { columnOptions },
        } = normalize(payload, columnOptionsListSchema);

        if (!state.columnOptions) {
            return update(state, {
                columnOptions: { $set: columnOptions },
            });
        }

        return update(state, {
            columnOptions: { $merge: columnOptions },
        });
    },
    updateSolutionSettingsReducer = (state, { payload, fromAction }) =>
        update(state, {
            settings: { $merge: payload },
            ...setPending(fromAction.type, false),
        }),
    updateActionSettingsReducer = (state, { payload, fromAction }) => {
        const {
                entities: { actionSettings },
            } = normalize(payload, actionSettingsListSchema),
            applicationPermissionsAssigned = dot.pick(
                "Email.applicationPermissionsAssigned",
                actionSettings
            ),
            temp = update(state, {
                actionSettings: { $set: actionSettings },
                applicationPermissionsAssigned: {
                    $set: applicationPermissionsAssigned,
                },
                $unset: ["actionPollingTimeout"],
                ...setPending(fromAction.type, false),
            });
        return update(
            temp,
            setPending(epics.actionTypes.pollActionSettings, false)
        );
    },
    updateActionPollingTimeoutReducer = (state) => {
        return update(state, {
            actionPollingTimeout: { $set: true },
            ...setPending(epics.actionTypes.pollActionSettings, false),
        });
    },
    updateActiveDeviceGroupReducer = (state, { payload }) => {
        if (state.deviceGroups[payload]) {
            return update(state, { activeDeviceGroupId: { $set: payload } });
        }

        const deviceGroupId = Object.keys(state.deviceGroups)[0];
        if (deviceGroupId) {
            return update(state, {
                activeDeviceGroupId: { $set: deviceGroupId },
            });
        }

        return state;
    },
    updateThemeReducer = (state, { payload }) =>
        update(state, { theme: { $set: payload } }),
    updateTimeInterval = (state, { payload }) =>
        update(state, { timeInterval: { $set: payload } }),
    logoReducer = (state, { payload, fromAction }) =>
        update(state, {
            logo: { $set: payload.logo ? payload.logo : svgs.mmmLogo },
            name: { $set: payload.name ? payload.name : "header.companyName" },
            isDefaultLogo: { $set: payload.logo ? false : true },
            ...setPending(fromAction.type, false),
        }),
    releaseReducer = (state, { payload }) =>
        update(state, {
            version: { $set: payload.version },
            releaseNotesUrl: { $set: payload.releaseNotesUrl },
        }),
    grafanaUrlReducer = (state, { payload }) =>
        update(state, {
            grafanaUrl: {
                $set: payload,
            },
        }),
    grafanaOrgIdReducer = (state, { payload }) =>
        update(state, {
            grafanaOrgId: {
                $set: payload,
            },
        }),
    setDeviceGroupFlyoutReducer = (state, { payload }) =>
        update(state, {
            deviceGroupFlyoutIsOpen: { $set: !!payload },
        }),
    setCreateDeviceQueryFlyoutReducer = (state, { payload }) =>
        update(state, {
            createDeviceQueryFlyoutIsOpen: { $set: !!payload },
        }),
    setActiveDeviceQueryConditionsReducer = (state, { payload }) =>
        update(state, {
            activeDeviceQueryConditions: { $set: payload },
        }),
    updateCurrentWindow = (state, { payload }) =>
        update(state, { currentWindow: { $set: payload } }),
    checkTenantAndSwitchReducer = (state, { payload }) => {
        let urlTenant = payload.tenantId;
        const currentTenant = state.user.tenant;
        const availableTenants = state.user.availableTenants;
        if (urlTenant && currentTenant !== urlTenant) {
            if (availableTenants.has(urlTenant)) {
                // Put in local storage
                HttpClient.setLocalStorageValue(
                    "redirectUrl",
                    payload.redirectUrl
                );
                // Switch Tenant
                TenantService.tenantIsDeployed(urlTenant).subscribe(
                    (response) => {
                        if (response) {
                            AuthService.switchTenant(urlTenant);
                        } else {
                            alert(
                                "The tenant you're trying to switch to is not fully deployed. Please wait a few minutes before trying to access your new tenant."
                            );
                        }
                    },
                    (error) => {
                        alert(
                            "An error ocurred while trying to switch tenants."
                        );
                    }
                );
            } else {
                alert("Tenant doesn't exist or you do not have access");
                window.history.back();
            }
        }
        return update(state, {});
    },
    /* Action types that cause a pending flag */
    fetchableTypes = [
        epics.actionTypes.fetchSelectedDeviceGroup,
        epics.actionTypes.fetchDeviceGroups,
        epics.actionTypes.fetchDeviceGroupFilters,
        epics.actionTypes.updateLogo,
        epics.actionTypes.fetchLogo,
        epics.actionTypes.fetchActionSettings,
        epics.actionTypes.pollActionSettings,
        epics.actionTypes.fetchSolutionSettings,
        epics.actionTypes.fetchTelemetryStatus,
        epics.actionTypes.fetchAlerting,
        epics.actionTypes.fetchColumnMappings,
        epics.actionTypes.fetchColumnOptions,
    ];

export const redux = createReducerScenario({
    updateUser: { type: "APP_USER_UPDATE", reducer: updateUserReducer },
    updateAlerting: {
        type: "APP_ALERTING_UPDATE",
        reducer: updateAlertingReducer,
    },
    updateTelemetryProperties: {
        type: "APP_UPDATE_TELEMETRY_STATUS",
        reducer: updateTelemetryPropertiesReducer,
    },
    updateDeviceGroups: {
        type: "APP_DEVICE_GROUP_UPDATE",
        reducer: updateDeviceGroupsReducer,
    },
    deleteDeviceGroups: {
        type: "APP_DEVICE_GROUP_DELETE",
        reducer: deleteDeviceGroupsReducer,
    },
    insertDeviceGroups: {
        type: "APP_DEVICE_GROUP_INSERT",
        reducer: insertDeviceGroupsReducer,
    },
    updateActiveDeviceGroup: {
        type: "APP_ACTIVE_DEVICE_GROUP_UPDATE",
        reducer: updateActiveDeviceGroupReducer,
    },
    updateColumnMappings: {
        type: "APP_COLUMN_MAPPINGS_GETCH",
        reducer: updateColumnMappingsReducer,
    },
    deleteColumnMappings: {
        type: "APP_COLUMN_MAPPINGS_DELETE",
        reducer: deleteColumnMappingsReducer,
    },
    updateColumnOptions: {
        type: "APP_COLUMN_OPTIONS_FETCH",
        reducer: updateColumnOptionsReducer,
    },
    insertColumnOptions: {
        type: "APP_COLUMN_OPTIONS_INSERT",
        reducer: insertColumnOptionsReducer,
    },
    changeTheme: { type: "APP_CHANGE_THEME", reducer: updateThemeReducer },
    registerError: { type: "APP_REDUCER_ERROR", reducer: errorReducer },
    updateLogo: { type: "APP_UPDATE_LOGO", reducer: logoReducer },
    updateSolutionSettings: {
        type: "APP_UPDATE_SOLUTION_SETTINGS",
        reducer: updateSolutionSettingsReducer,
    },
    updateActionSettings: {
        type: "APP_UPDATE_ACTION_SETTINGS",
        reducer: updateActionSettingsReducer,
    },
    updateActionPollingTimeout: {
        type: "APP_UPDATE_ACTION_POLLING_TIMEOUT",
        reducer: updateActionPollingTimeoutReducer,
    },
    getReleaseInformation: { type: "APP_GET_VERSION", reducer: releaseReducer },
    getGrafanaUrl: {
        type: "APP_GET_GRAfANA_URL",
        reducer: grafanaUrlReducer,
    },
    getGrafanaOrgId: {
        type: "APP_GET_GRAfANA_ORG",
        reducer: grafanaOrgIdReducer,
    },
    setDeviceGroupFlyoutStatus: {
        type: "APP_SET_DEVICE_GROUP_FLYOUT_STATUS",
        reducer: setDeviceGroupFlyoutReducer,
    },
    setActiveDeviceQueryConditions: {
        type: "APP_DEVICE_QUERY_CONDITIONS_UPDATE",
        reducer: setActiveDeviceQueryConditionsReducer,
    },
    setCreateDeviceQueryFlyoutStatus: {
        type: "APP_SET_CREATE_DEVICE_QUERY_FLYOUT_STATUS",
        reducer: setCreateDeviceQueryFlyoutReducer,
    },
    updateTimeInterval: {
        type: "APP_UPDATE_TIME_INTERVAL",
        reducer: updateTimeInterval,
    },
    updateCurrentWindow: {
        type: "APP_UPDATE_CURRENT_WINDOW",
        reducer: updateCurrentWindow,
    },
    isFetching: { multiType: fetchableTypes, reducer: pendingReducer },
    checkTenantAndSwitch: {
        type: "CHECK_TENANT_AND_SWITCH",
        reducer: checkTenantAndSwitchReducer,
    },
});

export const reducer = { app: redux.getReducer(initialState) };
// ========================= Reducers - END

// ========================= Selectors - START
export const getAppReducer = (state) => state.app;
export const getVersion = (state) => getAppReducer(state).version;
export const getTheme = (state) => getAppReducer(state).theme;
export const getTimeSeriesExplorerUrl = (state) =>
    getAppReducer(state).timeSeriesExplorerUrl;
export const getDeviceGroupEntities = (state) =>
    getAppReducer(state).deviceGroups;
export const getActiveDeviceGroupId = (state) =>
    getAppReducer(state).activeDeviceGroupId;
export const getActiveDeviceQueryConditions = (state) =>
    getAppReducer(state).activeDeviceQueryConditions;
export const getSettings = (state) => getAppReducer(state).settings;
export const getAzureMapsKey = (state) => getSettings(state).azureMapsKey;
export const getDiagnosticsOptIn = (state) =>
    getSettings(state).diagnosticsOptIn;
export const getDeviceGroupFlyoutStatus = (state) =>
    getAppReducer(state).deviceGroupFlyoutIsOpen;
export const getCreateDeviceQueryFlyoutStatus = (state) =>
    getAppReducer(state).createDeviceQueryFlyoutIsOpen;
export const getDeviceGroupsError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.fetchDeviceGroups);
export const getDeviceGroupsPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchDeviceGroups);
export const getSolutionSettingsError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.fetchSolutionSettings);
export const getSolutionSettingsPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchSolutionSettings);
export const getAlerting = (state) => getAppReducer(state).alerting;
export const getDeviceGroups = createSelector(
    getDeviceGroupEntities,
    (deviceGroups) => Object.keys(deviceGroups).map((id) => deviceGroups[id])
);
export const getActiveDeviceGroup = createSelector(
    getDeviceGroupEntities,
    getActiveDeviceGroupId,
    (deviceGroups, activeGroupId) => deviceGroups[activeGroupId]
);
export const getActiveDeviceGroupConditions = createSelector(
    getActiveDeviceGroup,
    (activeDeviceGroup) => (activeDeviceGroup || {}).conditions
);
export const getActiveDeviceGroupMappingId = createSelector(
    getActiveDeviceGroup,
    (activeDeviceGroup) => (activeDeviceGroup || {}).mappingId
);
export const getColumnMappings = (state) =>
    getAppReducer(state).columnMappings || [];
export const getColumnOptions = (state) =>
    getAppReducer(state).columnOptions || [];
export const getColumnOptionsPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchColumnOptions);
export const getActiveDeviceGroupMapping = createSelector(
    getColumnMappings,
    getActiveDeviceGroupMappingId,
    (mappings, mappingId) => mappings[mappingId]
);
export const getColumnMappingsList = createSelector(
    getColumnMappings,
    (columnMappings) =>
        Object.keys(columnMappings)
            .filter(function (elem) {
                //return false for the element that matches Default
                return elem !== "Default";
            })
            .map((id) => columnMappings[id])
);
export const getColumnOptionsList = createSelector(
    getColumnOptions,
    (columnOptions) =>
        Object.keys(columnOptions).map(
            (deviceGroupId) => columnOptions[deviceGroupId]
        )
);
export const getDefaultColumnMapping = createSelector(
    getColumnMappings,
    (mappings) => mappings["Default"]
);
export const getColumnMappingById = (state, id) => getColumnMappings(state)[id];
export const getColumnMappingPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchColumnMappings);

export const getLogo = (state) => getAppReducer(state).logo;
export const getName = (state) => getAppReducer(state).name;
export const isDefaultLogo = (state) => getAppReducer(state).isDefaultLogo;
export const getReleaseNotes = (state) => getAppReducer(state).releaseNotesUrl;
export const getGrafanaUrl = (state) => getAppReducer(state).grafanaUrl;
export const getGrafanaOrgId = (state) => getAppReducer(state).grafanaOrgId;
export const setLogoError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.updateLogo);
export const setLogoPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.updateLogo);
export const getLogoError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.fetchLogo);
export const getDeviceGroupError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.fetchDeviceGroups);
export const getLogoPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchLogo);

export const getTimeInterval = (state) => getAppReducer(state).timeInterval;

export const getUser = (state) => getAppReducer(state).user;
export const getSessionId = (state) => getAppReducer(state).sessionId;
export const getCurrentWindow = (state) => getAppReducer(state).currentWindow;

export const getActionSettings = (state) => getAppReducer(state).actionSettings;
export const getActionSettingsPendingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.fetchActionSettings);
export const getActionSettingsError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.fetchActionSettings);

export const getActionPollingStatus = (state) =>
    getPending(getAppReducer(state), epics.actionTypes.pollActionSettings);
export const getActionPollingError = (state) =>
    getError(getAppReducer(state), epics.actionTypes.pollActionSettings);

export const getActionPollingTimeout = (state) =>
    getAppReducer(state).actionPollingTimeout;

export const getApplicationPermissionsAssigned = (state) =>
    getAppReducer(state).applicationPermissionsAssigned;

export const getUserCurrentTenant = (state) => getAppReducer(state).user.tenant;

// ========================= Selectors - END
