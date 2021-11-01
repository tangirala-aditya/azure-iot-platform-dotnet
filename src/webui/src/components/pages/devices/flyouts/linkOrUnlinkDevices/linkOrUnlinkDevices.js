// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { permissions, toDeviceDiagnosticsModel } from "services/models";
import { LinkedComponent, stringToBoolean, svgs } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    FormControl,
    FormGroup,
    FormLabel,
    Protected,
    Radio,
} from "components/shared";
import { IoTHubManagerService } from "services";

const linkOrUnlinkOptions = {
    linking: {
        hintName: "devices.flyouts.linkOrUnlinkDevice.linkDevice.hint",
        value: true,
    },
    unLinking: {
        hintName: "devices.flyouts.linkOrUnlinkDevice.unlinkDevice.hint",
        value: false,
    },
};

export class LinkOrUnlinkDevice extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            isDeviceExplorer: true,
            message: undefined,
            formData: {
                isLinkSelected: true,
                selectedEdgeDeviceId: "",
            },
        };

        // Linked components
        this.formDataLink = this.linkTo("formData");

        this.edgeDeviceLink = this.formDataLink
            .forkTo("selectedEdgeDeviceId")
            .map(({ value }) => value);

        this.linkOrUnlinkDeviceLink = this.formDataLink
            .forkTo("isLinkSelected")
            .map(stringToBoolean);
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentWillMount() {
        if (
            this.props &&
            this.props.location &&
            this.props.location.pathname === "/devices"
        ) {
            this.setState({
                isDeviceExplorer: true,
            });
        } else {
            this.setState({
                isDeviceExplorer: false,
            });
        }
    }

    componentDidMount() {
        this.fetchEdgeDevices();
    }

    fetchEdgeDevices = () => {
        IoTHubManagerService.getDevicesByQuery(
            "capabilities.iotEdge=true"
        ).subscribe((edgeDevices) => {
            this.setState({
                edgeDeviceOptions: edgeDevices
                    ? edgeDevices.items.map((device) => ({
                          label: device.id,
                          value: device.id,
                      }))
                    : [],
            });
        });
    };

    formIsValid() {
        return [this.linkOrUnlinkDeviceLink, this.edgeDeviceLink].every(
            (link) => !link.error
        );
    }

    linkOrUnlinkChange = ({ target: { value } }) => {
        if (value === "true") {
            this.setState({
                formData: {
                    ...this.state.formData,
                    isLinkSelected: true,
                },
            });
        } else {
            this.setState({
                formData: {
                    ...this.state.formData,
                    isLinkSelected: false,
                },
            });
        }
        this.formControlChange();
    };

    formControlChange = () => {
        if (this.state.changesApplied) {
            this.setState({
                successCount: 0,
                changesApplied: false,
            });
        }
    };

    onFlyoutClose = (eventName) => {
        this.props.logEvent(
            toDeviceDiagnosticsModel(eventName, this.state.formData)
        );
        this.props.onClose();
    };

    onEdgeDeviceSelected = ({ target: { value } }) => {
        this.setState({
            selectedEdgeDeviceId: value,
        });
    };

    apply = (event) => {
        event.preventDefault();
        const { formData } = this.state;
        var selectedDeviceIds = [];
        this.props.selectedDevices.map((device) =>
            selectedDeviceIds.push(device.id)
        );
        if (this.formIsValid()) {
            this.setState({ isPending: true, error: null });
            if (
                selectedDeviceIds.length !== 0 &&
                selectedDeviceIds.length < 5
            ) {
                if (formData.isLinkSelected) {
                    IoTHubManagerService.linkSelectedDevicesToEdge(
                        selectedDeviceIds,
                        formData.selectedEdgeDeviceId
                    ).subscribe(
                        function (response) {
                            if (response.isSuccessful) {
                                this.setState({
                                    message:
                                        "Selected devices linked successfully",
                                });
                            } else {
                                this.setState({
                                    error: {
                                        message: response.validationMessages[0],
                                    },
                                    isPending: false,
                                    changesApplied: false,
                                });
                            }
                            this.props.fetchDevices();
                        }.bind(this),
                        (error) =>
                            this.setState({
                                error,
                                isPending: false,
                                changesApplied: false,
                            }), // On Error
                        () =>
                            this.setState({
                                isPending: false,
                                changesApplied: true,
                            }) // On Completed
                    );
                } else {
                    IoTHubManagerService.unlinkSelectedDevices(
                        selectedDeviceIds
                    ).subscribe(
                        function (response) {
                            if (response.isSuccessful) {
                                this.setState({
                                    message:
                                        "Selected devices unlinked successfully",
                                });
                            } else {
                                this.setState({
                                    error: {
                                        message: response.validationMessages[0],
                                    },
                                    isPending: false,
                                    changesApplied: false,
                                });
                            }
                            this.props.fetchDevices();
                        }.bind(this),
                        (error) =>
                            this.setState({
                                error,
                                isPending: false,
                                changesApplied: false,
                            }), // On Error
                        () =>
                            this.setState({
                                isPending: false,
                                changesApplied: true,
                            }) // On Completed
                    );
                }
            } else {
                this.setState({
                    error: "Selected devices count should not be greater than 200",
                    isPending: false,
                    changesApplied: true,
                });
            }
            this.props.logEvent(
                toDeviceDiagnosticsModel("Devices_Linking_Click", formData)
            );
        }
    };

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
        const { t } = this.props,
            { isPending, error, changesApplied } = this.state;

        return (
            <Flyout
                header={
                    this.state.isDeviceExplorer
                        ? t(
                              "devices.flyouts.linkOrUnlinkDevice.linkOrUnlinkDevicestitle"
                          )
                        : t(
                              "devices.flyouts.linkOrUnlinkDevice.linkDevicestitle"
                          )
                }
                t={t}
                onClose={() => this.onFlyoutClose("Devices_TopXCloseClick")}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <div>
                    {this.state.isDeviceExplorer
                        ? t(
                              "devices.flyouts.linkOrUnlinkDevice.linkOrUnlinkDevicesDescription"
                          )
                        : t(
                              "devices.flyouts.linkOrUnlinkDevice.linkDevicesDescription"
                          )}
                </div>
                <Protected permission={permissions.createDevices}>
                    <form
                        className="devices-new-container"
                        onSubmit={this.apply}
                    >
                        <div className="devices-new-content">
                            <FormGroup>
                                <Radio
                                    id="link-device"
                                    link={this.linkOrUnlinkDeviceLink}
                                    value={linkOrUnlinkOptions.linking.value}
                                    onChange={this.linkOrUnlinkChange}
                                    disabled={changesApplied}
                                >
                                    {t(linkOrUnlinkOptions.linking.hintName)}
                                </Radio>
                                {this.state.isDeviceExplorer && (
                                    <Radio
                                        id="unlink-device"
                                        link={this.linkOrUnlinkDeviceLink}
                                        value={
                                            linkOrUnlinkOptions.unLinking.value
                                        }
                                        onChange={this.linkOrUnlinkChange}
                                        disabled={changesApplied}
                                    >
                                        {t(
                                            linkOrUnlinkOptions.unLinking
                                                .hintName
                                        )}
                                    </Radio>
                                )}
                            </FormGroup>
                            {this.state.formData.isLinkSelected && (
                                <FormGroup>
                                    <FormLabel isRequired="true">
                                        {t("linkDeviceGroupGateway.edgeDevice")}
                                    </FormLabel>
                                    <FormControl
                                        type="select"
                                        ariaLabel={t(
                                            "devices.flyouts.linkOrUnlinkDevice.edgeDeviceLabel"
                                        )}
                                        className="long"
                                        link={this.edgeDeviceLink}
                                        onChange={this.onEdgeDeviceSelected}
                                        options={this.state.edgeDeviceOptions}
                                        placeholder={t(
                                            "devices.flyouts.linkOrUnlinkDevice.edgeDevicePlaceholder"
                                        )}
                                        clearable={false}
                                        searchable={false}
                                        disabled={changesApplied}
                                    />
                                </FormGroup>
                            )}
                        </div>
                        {error && (
                            <AjaxError
                                className="devices-new-error"
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
                                <br />
                                <div>{this.state.message}</div>
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
