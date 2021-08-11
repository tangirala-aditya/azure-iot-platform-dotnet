// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { IoTHubManagerService } from "services";
import { permissions, toDiagnosticsModel } from "services/models";
import { Btn, Protected } from "components/shared";
import { svgs, LinkedComponent } from "utilities";
import Flyout from "components/shared/flyout";
import DeviceGroupForm from "./views/deviceGroupForm";
import DeviceGroups from "./views/deviceGroups";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./manageDeviceGroups.module.scss"));

const toOption = (value, label) => ({
    label: label || value,
    value,
});
const toColumnMappingOptions = (mapping) => ({
    label: mapping.name,
    value: mapping.id,
});

export class ManageDeviceGroups extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            addNewDeviceGroup: false,
            selectedDeviceGroup: undefined,
            filterOptions: [],
            filtersError: undefined,
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        this.subscription = IoTHubManagerService.getDeviceProperties().subscribe(
            (items) => {
                const filterOptions = items.map((item) => toOption(item));
                this.setState({ filterOptions });
            },
            (filtersError) => this.setState({ filtersError })
        );
        const columnMappingsOptions = [
            ...this.props.columnMappings,
        ].map((item) => toColumnMappingOptions(item));
        this.setState({ columnMappingsOptions });
    }

    componentWillUnmount() {
        if (this.subscription) this.subscription.unsubscribe();
    }

    toggleNewFilter = () => {
        if (!this.state.addNewDeviceGroup) {
            this.props.logEvent(toDiagnosticsModel("DeviceGroup_NewClick", {}));
        }
        this.setState({ addNewDeviceGroup: !this.state.addNewDeviceGroup });
    };

    closeForm = () =>
        this.setState({
            addNewDeviceGroup: false,
            selectedDeviceGroup: undefined,
        });

    onEditDeviceGroup = (selectedDeviceGroup) => () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_EditClick", {}));
        this.setState({ selectedDeviceGroup });
    };

    onCloseFlyout = () => {
        this.props.logEvent(
            toDiagnosticsModel("DeviceGroup_TopXCloseClick", {})
        );
        this.props.closeFlyout();
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
        const { t, deviceGroups = [] } = this.props;
        const btnStyle = {
            margin: "0px",
            paddingLeft: "10px",
        };
        return (
            <Flyout.Container
                header={t("deviceGroupsFlyout.title")}
                t={t}
                onClose={this.onCloseFlyout}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <div className={css("manage-filters-flyout-container")}>
                    {this.state.addNewDeviceGroup ||
                    !!this.state.selectedDeviceGroup ? (
                        <DeviceGroupForm
                            {...this.props}
                            {...this.state}
                            cancel={this.closeForm}
                        />
                    ) : (
                        <div>
                            <Protected
                                permission={permissions.createDeviceGroups}
                            >
                                <Btn
                                    className={css("add-btn")}
                                    style={btnStyle}
                                    svg={svgs.plus}
                                    onClick={this.toggleNewFilter}
                                >
                                    {t("deviceGroupsFlyout.create")}
                                </Btn>
                            </Protected>
                            {deviceGroups.length > 0 && (
                                <DeviceGroups
                                    {...this.props}
                                    onEditDeviceGroup={this.onEditDeviceGroup}
                                />
                            )}
                        </div>
                    )}
                </div>
            </Flyout.Container>
        );
    }
}
