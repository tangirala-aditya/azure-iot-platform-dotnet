// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import update from "immutability-helper";

import { DeviceSimulationService, IoTHubManagerService } from "services";
import {
    authenticationTypeOptions,
    permissions,
    toNewDeviceRequestModel,
    toSinglePropertyDiagnosticsModel,
    toDeviceDiagnosticsModel,
    toDiagnosticsModel,
} from "services/models";
import {
    copyToClipboard,
    int,
    isEmptyObject,
    LinkedComponent,
    stringToBoolean,
    svgs,
    Validator,
} from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    FormControl,
    FormGroup,
    FormLabel,
    FormSection,
    Indicator,
    Protected,
    Radio,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummaryCount,
    SummarySection,
    Svg,
} from "components/shared";

import Config from "app.config";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./deviceNew.module.scss"));

const isIntRegex = /^-?\d*$/,
    nonInteger = (x) => !x.match(isIntRegex),
    stringToInt = (x) => (x === "" || x === "-" ? x : int(x)),
    deviceOptions = {
        labelName: "devices.flyouts.new.device.label",
        device: {
            labelName: "devices.flyouts.new.device.device",
            value: false,
        },
        edgeDevice: {
            labelName: "devices.flyouts.new.device.edgeDevice",
            value: true,
        },
    },
    deviceTypeOptions = {
        labelName: "devices.flyouts.new.deviceType.label",
        simulated: {
            labelName: "devices.flyouts.new.deviceType.simulated",
            value: true,
        },
        physical: {
            labelName: "devices.flyouts.new.deviceType.physical",
            value: false,
        },
    },
    deviceIdTypeOptions = {
        labelName: "devices.flyouts.new.deviceId.label",
        manual: {
            hintName: "devices.flyouts.new.deviceId.hint",
            value: false,
        },
        generate: {
            labelName: "devices.flyouts.new.deviceId.sysGenerated",
            value: true,
        },
    },
    authTypeOptions = {
        labelName: "devices.flyouts.new.authenticationType.label",
        symmetric: {
            labelName: "devices.flyouts.new.authenticationType.symmetric",
            value: authenticationTypeOptions.symmetric,
        },
        x509: {
            labelName: "devices.flyouts.new.authenticationType.x509",
            value: authenticationTypeOptions.x509,
        },
    },
    authKeyTypeOptions = {
        labelName: "devices.flyouts.new.authenticationKey.label",
        generate: {
            labelName: "devices.flyouts.new.authenticationKey.generateKeys",
            value: true,
        },
        manual: {
            labelName: "devices.flyouts.new.authenticationKey.manualKeys",
            value: false,
        },
    },
    DeviceDetail = ({ label, value }) => (
        <FormSection className={css("device-detail")}>
            <SectionHeader>{label}</SectionHeader>
            <div className={css("device-detail-contents")}>
                <div className={css("device-detail-value")}>{value}</div>
                <Svg
                    className={css("copy-icon")}
                    src={svgs.copy}
                    onClick={() => copyToClipboard(value)}
                />
            </div>
        </FormSection>
    ),
    DeviceConnectionString = ({
        label,
        deviceId,
        hostName,
        sharedAccessKey,
    }) => (
        <DeviceDetail
            label={label}
            value={`HostName=${hostName};DeviceId=${deviceId};SharedAccessKey=${sharedAccessKey}`}
        />
    ),
    ProvisionedDevice = ({ device, t }) => {
        // When an error occurs, the device has no data... and so there is nothing to display here.
        if (isEmptyObject(device)) {
            return null;
        }

        const {
            id,
            iotHubHostName: hostName,
            authentication: { primaryKey },
            authentication: { secondaryKey },
        } = device;

        return (
            <div>
                <DeviceDetail
                    label={t("devices.flyouts.new.deviceId.label")}
                    value={id}
                />

                {device.authentication.authenticationType !==
                    authTypeOptions.x509.value && (
                    <div>
                        <DeviceDetail
                            label={t(
                                "devices.flyouts.new.authenticationKey.primaryKey"
                            )}
                            value={primaryKey}
                        />
                        <DeviceDetail
                            label={t(
                                "devices.flyouts.new.authenticationKey.secondaryKey"
                            )}
                            value={secondaryKey}
                        />
                        <DeviceConnectionString
                            label={t(
                                "devices.flyouts.new.authenticationKey.primaryKeyConnection"
                            )}
                            deviceId={id}
                            hostName={hostName}
                            sharedAccessKey={primaryKey}
                        />
                        <DeviceConnectionString
                            label={t(
                                "devices.flyouts.new.authenticationKey.secondaryKeyConnection"
                            )}
                            deviceId={id}
                            hostName={hostName}
                            sharedAccessKey={secondaryKey}
                        />
                    </div>
                )}
            </div>
        );
    };

export class DeviceNew extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            formData: {
                // start the form as a physical (simulated === false) edge device
                count: 1,
                deviceId: "",
                isEdgeDevice: deviceOptions.edgeDevice.value,
                isGenerateId: deviceIdTypeOptions.manual.value,
                isSimulated: deviceTypeOptions.physical.value,
                deviceModel: undefined,
                authenticationType: authTypeOptions.symmetric.value,
                isGenerateKeys: authKeyTypeOptions.generate.value,
                primaryKey: undefined,
                secondaryKey: undefined,
            },
            provisionedDevice: {},
            expandedValue: false,
        };

        if (props.deviceModelOptions === undefined) {
            props.fetchDeviceModelOptions();
        }

        // Linked components
        this.formDataLink = this.linkTo("formData");

        this.deviceLink = this.formDataLink
            .forkTo("isEdgeDevice")
            .map(stringToBoolean);

        this.deviceTypeLink = this.formDataLink
            .forkTo("isSimulated")
            .map(stringToBoolean);

        this.countLink = this.formDataLink
            .forkTo("count")
            .reject(nonInteger)
            .map(stringToInt)
            .check(Validator.notEmpty, () =>
                this.props.t("devices.flyouts.new.validation.required")
            )
            .check(
                (num) => num > 0,
                () =>
                    this.props.t(
                        "devices.flyouts.new.validation.greaterThanZero"
                    )
            );

        this.isGenerateIdLink = this.formDataLink
            .forkTo("isGenerateId")
            .map(stringToBoolean);

        this.deviceIdLink = this.formDataLink.forkTo("deviceId").check(
            (devId) =>
                !this.deviceTypeLink.value && !this.isGenerateIdLink.value
                    ? Validator.notEmpty(devId)
                    : true,
            () => this.props.t("devices.flyouts.new.validation.required")
        );

        this.deviceModelLink = this.formDataLink
            .forkTo("deviceModel")
            .map(({ value }) => value)
            .check(
                (devModel) =>
                    this.deviceTypeLink.value
                        ? Validator.notEmpty(devModel)
                        : true,
                () => this.props.t("devices.flyouts.new.validation.required")
            );

        this.authenticationTypeLink = this.formDataLink
            .forkTo("authenticationType")
            .reject(nonInteger)
            .map(stringToInt);

        this.isGenerateKeysLink = this.formDataLink
            .forkTo("isGenerateKeys")
            .map(stringToBoolean);

        this.primaryKeyLink = this.formDataLink.forkTo("primaryKey").check(
            (priKey) =>
                !this.deviceTypeLink.value && !this.isGenerateKeysLink.value
                    ? Validator.notEmpty(priKey)
                    : true,
            () => this.props.t("devices.flyouts.new.validation.required")
        );

        this.secondaryKeyLink = this.formDataLink.forkTo("secondaryKey").check(
            (secKey) =>
                !this.deviceTypeLink.value && !this.isGenerateKeysLink.value
                    ? Validator.notEmpty(secKey)
                    : true,
            () => this.props.t("devices.flyouts.new.validation.required")
        );
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentWillUnmount() {
        if (this.provisionSubscription) {
            this.provisionSubscription.unsubscribe();
        }
    }

    shouldComponentUpdate(nextProps, nextState) {
        const { formData } = nextState;

        // When the device type is Physical, only allow 1 to be created
        if (
            formData.isSimulated === deviceTypeOptions.physical.value &&
            formData.count !== 1
        ) {
            this.setState(
                update(nextState, {
                    formData: { count: { $set: 1 } },
                })
            );
        }

        // When the authentication type is X.509, ensure keys to be entered manually
        if (
            formData.authenticationType === authTypeOptions.x509.value &&
            formData.isGenerateKeys !== authKeyTypeOptions.manual.value
        ) {
            this.setState(
                update(nextState, {
                    formData: {
                        isGenerateKeys: {
                            $set: authKeyTypeOptions.manual.value,
                        },
                    },
                })
            );
        }

        // Update normally
        return true;
    }

    formIsValid() {
        return [
            this.deviceLink,
            this.deviceTypeLink,
            this.countLink,
            this.deviceIdLink,
            this.isGenerateIdLink,
            this.deviceModelLink,
            this.authenticationTypeLink,
            this.isGenerateKeysLink,
            this.primaryKeyLink,
            this.secondaryKeyLink,
        ].every((link) => !link.error);
    }

    deviceChange = ({ target: { value } }) => {
        this.props.logEvent(
            toSinglePropertyDiagnosticsModel(
                "Devices_DeviceSelect",
                "Device",
                value === "true"
                    ? Config.device.edgeDevice
                    : Config.device.device
            )
        );
        if (value === "true") {
            this.setState({
                formData: {
                    ...this.state.formData,
                    isEdgeDevice: true,
                    isSimulated: false,
                },
            });
        } else {
            this.setState({
                formData: {
                    ...this.state.formData,
                    isEdgeDevice: false,
                    isSimulated: false,
                },
            });
        }
        this.formControlChange();
    };

    deviceTypeChange = ({ target: { value } }) => {
        this.props.logEvent(
            toSinglePropertyDiagnosticsModel(
                "Devices_DeviceTypeSelect",
                "DeviceType",
                value === "true"
                    ? Config.deviceType.simulated
                    : Config.deviceType.physical
            )
        );
        this.formControlChange();
    };

    onAuthenticationTypeChange = ({ target: { value } }) => {
        this.formControlChange();
        this.props.logEvent(
            toSinglePropertyDiagnosticsModel(
                "Devices_AuthTypeSelect",
                "AuthType",
                value === 0
                    ? Config.authenticationType.symmetricKey
                    : Config.authenticationType.x509
            )
        );
    };

    onAuthenticationKeyChange = ({ target: { value } }) => {
        this.formControlChange();
        this.props.logEvent(
            toSinglePropertyDiagnosticsModel(
                "Devices_AuthKeySelect",
                "AuthKey",
                value === "true"
                    ? Config.authenticationKey.autoKey
                    : Config.authenticationKey.manualKey
            )
        );
    };

    formControlChange = () => {
        if (this.state.changesApplied) {
            this.setState({
                successCount: 0,
                changesApplied: false,
                provisionedDevice: {},
            });
        }
    };

    onFlyoutClose = (eventName) => {
        this.props.logEvent(
            toDeviceDiagnosticsModel(eventName, this.state.formData)
        );
        this.props.onClose();
    };

    apply = (event) => {
        event.preventDefault();
        const { formData } = this.state;

        if (this.formIsValid()) {
            this.setState({ isPending: true, error: null });

            if (this.provisionSubscription) {
                this.provisionSubscription.unsubscribe();
            }

            this.props.logEvent(
                toDeviceDiagnosticsModel("Devices_ApplyClick", formData)
            );

            if (this.state.formData.isSimulated) {
                this.provisionSubscription =
                    DeviceSimulationService.incrementSimulatedDeviceModel(
                        formData.deviceModel,
                        formData.count
                    ).subscribe(
                        () => {
                            this.setState({
                                successCount: formData.count,
                                isPending: false,
                                changesApplied: true,
                            });
                            this.props.logEvent(
                                toSinglePropertyDiagnosticsModel(
                                    "Devices_Created",
                                    "DeviceType",
                                    Config.deviceType.simulated
                                )
                            );
                        },
                        (error) => {
                            this.setState({
                                error,
                                isPending: false,
                                changesApplied: true,
                            });
                        }
                    );
            } else {
                this.provisionSubscription =
                    IoTHubManagerService.provisionDevice(
                        toNewDeviceRequestModel(formData),
                        this.props.mapping
                    ).subscribe(
                        (response) => {
                            this.setState({
                                provisionedDevice: response.items[0],
                                successCount: formData.count,
                                isPending: false,
                                changesApplied: true,
                            });
                            this.props.insertDevices(response);
                            const metadata = {
                                DeviceType: Config.deviceType.physical,
                                DeviceID: this.state.provisionedDevice.id,
                            };
                            this.props.logEvent(
                                toDiagnosticsModel("Devices_Created", metadata)
                            );
                            this.props.fetchDeviceStatistics();
                        },
                        (error) => {
                            this.setState({
                                error,
                                isPending: false,
                                changesApplied: true,
                            });
                        }
                    );
            }
        }
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("devices.flyouts.new.pending");
        } else if (changesApplied) {
            return t("devices.flyouts.new.applySuccess");
        }
        return t("devices.flyouts.new.affected");
    }

    expandFlyout() {
        if (this.state.expandedValue) {
            this.setState({
                expandedValue: false,
            });
        } else {
            this.setState({
                expandedValue: true,
            });
        }
    }

    render() {
        const { t, deviceModelOptions } = this.props,
            {
                formData,
                provisionedDevice,
                isPending,
                error,
                successCount,
                changesApplied,
            } = this.state,
            isGenerateId =
                this.isGenerateIdLink.value ===
                deviceIdTypeOptions.generate.value,
            deviceName =
                this.deviceModelLink.value ||
                t("devices.flyouts.new.deviceIdExample.deviceName"),
            isEdgeDevice =
                this.deviceLink.value === deviceOptions.edgeDevice.value,
            isSimulatedDevice =
                this.deviceTypeLink.value === deviceTypeOptions.simulated.value,
            isX509 =
                this.authenticationTypeLink.value ===
                authTypeOptions.x509.value,
            isGenerateKeys =
                this.isGenerateKeysLink.value ===
                authKeyTypeOptions.generate.value,
            summaryCount = changesApplied ? successCount : formData.count,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage();

        return (
            <Flyout
                header={t("devices.flyouts.new.title")}
                t={t}
                onClose={() => this.onFlyoutClose("Devices_TopXCloseClick")}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <Protected permission={permissions.createDevices}>
                    <form
                        className={css("devices-new-container")}
                        onSubmit={this.apply}
                    >
                        <div className={css("devices-new-content")}>
                            <FormGroup>
                                <FormLabel>
                                    {t(deviceOptions.labelName)}
                                </FormLabel>
                                <Radio
                                    id="device-type-edge-device"
                                    link={this.deviceLink}
                                    value={deviceOptions.edgeDevice.value}
                                    onChange={this.deviceChange}
                                >
                                    {t(deviceOptions.edgeDevice.labelName)}
                                </Radio>
                                <Radio
                                    id="device-type-device"
                                    link={this.deviceLink}
                                    value={deviceOptions.device.value}
                                    onChange={this.deviceChange}
                                >
                                    {t(deviceOptions.device.labelName)}
                                </Radio>
                            </FormGroup>
                            {
                                // Disabled due to Simulation not being a feature of this release
                                isEdgeDevice && false && (
                                    <FormGroup>
                                        <FormLabel>
                                            {t(deviceTypeOptions.labelName)}
                                        </FormLabel>
                                        {/* <Radio
                    id="device-type-simulated"
                    link={this.deviceTypeLink}
                    value={deviceTypeOptions.simulated.value}
                    onChange={this.deviceTypeChange}>
                    {t(deviceTypeOptions.simulated.labelName)}
                  </Radio> */}
                                        <Radio
                                            id="device-type-real"
                                            link={this.deviceTypeLink}
                                            value={
                                                deviceTypeOptions.physical.value
                                            }
                                            onChange={this.deviceTypeChange}
                                        >
                                            {t(
                                                deviceTypeOptions.physical
                                                    .labelName
                                            )}
                                        </Radio>
                                    </FormGroup>
                                )
                            }
                            {isSimulatedDevice && (
                                <>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(
                                                "devices.flyouts.new.count.label"
                                            )}
                                        </FormLabel>
                                        <FormControl
                                            link={this.countLink}
                                            type="text"
                                            onChange={this.formControlChange}
                                        />
                                    </FormGroup>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(
                                                "devices.flyouts.new.deviceIdExample.label"
                                            )}
                                        </FormLabel>
                                        <div
                                            className={css("device-id-example")}
                                        >
                                            {t(
                                                "devices.flyouts.new.deviceIdExample.format",
                                                { deviceName }
                                            )}
                                        </div>
                                    </FormGroup>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(
                                                "devices.flyouts.new.deviceModel.label"
                                            )}
                                        </FormLabel>
                                        <FormControl
                                            link={this.deviceModelLink}
                                            ariaLabel={t(
                                                "devices.flyouts.new.deviceModel.label"
                                            )}
                                            type="select"
                                            options={deviceModelOptions}
                                            placeholder={t(
                                                "devices.flyouts.new.deviceModel.hint"
                                            )}
                                            onChange={this.formControlChange}
                                        />
                                    </FormGroup>
                                </>
                            )}
                            {!isSimulatedDevice && (
                                <>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(
                                                "devices.flyouts.new.count.label"
                                            )}
                                        </FormLabel>
                                        <div className={css("device-count")}>
                                            {this.countLink.value}
                                        </div>
                                    </FormGroup>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(
                                                "devices.flyouts.new.deviceId.label"
                                            )}
                                        </FormLabel>
                                        <Radio
                                            link={this.isGenerateIdLink}
                                            value={
                                                deviceIdTypeOptions.manual.value
                                            }
                                            onChange={this.formControlChange}
                                        >
                                            <FormControl
                                                id="device-manual-id"
                                                className={css("device-id")}
                                                link={this.deviceIdLink}
                                                disabled={isGenerateId}
                                                type="text"
                                                aria-label={t(
                                                    deviceIdTypeOptions.manual
                                                        .hintName
                                                )}
                                                placeholder={t(
                                                    deviceIdTypeOptions.manual
                                                        .hintName
                                                )}
                                                onChange={
                                                    this.formControlChange
                                                }
                                            />
                                        </Radio>
                                        <Radio
                                            id="device-auto-generated-id"
                                            link={this.isGenerateIdLink}
                                            value={
                                                deviceIdTypeOptions.generate
                                                    .value
                                            }
                                            onChange={this.formControlChange}
                                        >
                                            {t(
                                                deviceIdTypeOptions.generate
                                                    .labelName
                                            )}
                                        </Radio>
                                    </FormGroup>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(authTypeOptions.labelName)}
                                        </FormLabel>
                                        <Radio
                                            id="auth-symmetric"
                                            link={this.authenticationTypeLink}
                                            value={
                                                authTypeOptions.symmetric.value
                                            }
                                            onChange={
                                                this.onAuthenticationTypeChange
                                            }
                                        >
                                            {t(
                                                authTypeOptions.symmetric
                                                    .labelName
                                            )}
                                        </Radio>
                                        <Radio
                                            id="auth-x509"
                                            link={this.authenticationTypeLink}
                                            value={authTypeOptions.x509.value}
                                            onChange={
                                                this.onAuthenticationTypeChange
                                            }
                                        >
                                            {t(authTypeOptions.x509.labelName)}
                                        </Radio>
                                    </FormGroup>
                                    <FormGroup>
                                        <FormLabel>
                                            {t(authKeyTypeOptions.labelName)}
                                        </FormLabel>
                                        <Radio
                                            id="auth-auto-generate"
                                            link={this.isGenerateKeysLink}
                                            value={
                                                authKeyTypeOptions.generate
                                                    .value
                                            }
                                            disabled={isX509}
                                            onChange={
                                                this.onAuthenticationKeyChange
                                            }
                                        >
                                            {t(
                                                authKeyTypeOptions.generate
                                                    .labelName
                                            )}
                                        </Radio>
                                        <Radio
                                            id="auth-manual"
                                            link={this.isGenerateKeysLink}
                                            value={
                                                authKeyTypeOptions.manual.value
                                            }
                                            onChange={
                                                this.onAuthenticationKeyChange
                                            }
                                        >
                                            {t(
                                                authKeyTypeOptions.manual
                                                    .labelName
                                            )}
                                        </Radio>
                                        <FormGroup
                                            className={css("sub-settings")}
                                        >
                                            <FormLabel>
                                                {isX509
                                                    ? t(
                                                          "devices.flyouts.new.authenticationKey.primaryThumbprint"
                                                      )
                                                    : t(
                                                          "devices.flyouts.new.authenticationKey.primaryKey"
                                                      )}
                                            </FormLabel>
                                            <FormControl
                                                link={this.primaryKeyLink}
                                                disabled={isGenerateKeys}
                                                type="text"
                                                placeholder={t(
                                                    "devices.flyouts.new.authenticationKey.hint"
                                                )}
                                                onChange={
                                                    this.formControlChange
                                                }
                                            />
                                        </FormGroup>
                                        <FormGroup
                                            className={css("sub-settings")}
                                        >
                                            <FormLabel>
                                                {isX509
                                                    ? t(
                                                          "devices.flyouts.new.authenticationKey.secondaryThumbprint"
                                                      )
                                                    : t(
                                                          "devices.flyouts.new.authenticationKey.secondaryKey"
                                                      )}
                                            </FormLabel>
                                            <FormControl
                                                link={this.secondaryKeyLink}
                                                disabled={isGenerateKeys}
                                                type="text"
                                                placeholder={t(
                                                    "devices.flyouts.new.authenticationKey.hint"
                                                )}
                                                onChange={
                                                    this.formControlChange
                                                }
                                            />
                                        </FormGroup>
                                    </FormGroup>
                                </>
                            )}
                        </div>
                        <SummarySection>
                            <SectionHeader>
                                {t("devices.flyouts.new.summaryHeader")}
                            </SectionHeader>
                            <SummaryBody>
                                <SummaryCount>{summaryCount || 0}</SummaryCount>
                                <SectionDesc>{summaryMessage}</SectionDesc>
                                {this.state.isPending && <Indicator />}
                                {completedSuccessfully && (
                                    <Svg
                                        className={css("summary-icon")}
                                        src={svgs.apply}
                                    />
                                )}
                                {completedSuccessfully &&
                                    isSimulatedDevice &&
                                    t(
                                        "devices.flyouts.new.simulatedRefreshMessage"
                                    )}
                            </SummaryBody>
                        </SummarySection>

                        {error && (
                            <AjaxError
                                className={css("devices-new-error")}
                                t={t}
                                error={error}
                            />
                        )}
                        {!changesApplied && (
                            <BtnToolbar>
                                <Btn
                                    primary={true}
                                    disabled={isPending || !this.formIsValid()}
                                    type="submit"
                                >
                                    {t("devices.flyouts.new.apply")}
                                </Btn>
                                <Btn
                                    svg={svgs.cancelX}
                                    onClick={() =>
                                        this.onFlyoutClose(
                                            "Devices_CancelClick"
                                        )
                                    }
                                >
                                    {t("devices.flyouts.new.cancel")}
                                </Btn>
                            </BtnToolbar>
                        )}
                        {!!changesApplied && (
                            <>
                                <ProvisionedDevice
                                    device={provisionedDevice}
                                    t={t}
                                />
                                <BtnToolbar>
                                    <Btn
                                        svg={svgs.cancelX}
                                        onClick={() =>
                                            this.onFlyoutClose(
                                                "Devices_CloseClick"
                                            )
                                        }
                                    >
                                        {t("devices.flyouts.new.close")}
                                    </Btn>
                                </BtnToolbar>
                            </>
                        )}
                    </form>
                </Protected>
            </Flyout>
        );
    }
}
