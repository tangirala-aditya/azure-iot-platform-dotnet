// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { permissions, toDiagnosticsModel } from "services/models";
import { svgs, LinkedComponent, Validator } from "utilities";
import { DeviceGroupDelete } from "./deviceGroupDelete";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    // ComponentArray,
    FormControl,
    FormGroup,
    FormLabel,
    Indicator,
    Protected,
    Hyperlink,
} from "components/shared";
import { ConfigService } from "services";
import {
    toCreateDeviceGroupRequestModel,
    toUpdateDeviceGroupRequestModel,
} from "services/models";

import Flyout from "components/shared/flyout";
import { DeviceGroupTelemetryFormatContainer } from "../deviceGroupTelemetryFormat.container";
import { DeviceGroupSupportedMethodsContainer } from "../deviceGroupSupportedMethods.container";
import { NavLink } from "react-router-dom";
import deleteItem from "./delete-item.png";

const classnames = require("classnames/bind");
const css = classnames.bind(require("../manageDeviceGroups.module.scss"));
const Section = Flyout.Section;

// A counter for creating unique keys per new condition
let conditionKey = 0;

// Creates a state object for a condition
const newCondition = () => ({
        field: undefined,
        operator: undefined,
        type: undefined,
        value: "",
        key: conditionKey++, // Used by react to track the rendered elements
    }),
    operators = ["EQ", "GT", "LT", "GE", "LE"],
    valueTypes = ["Number", "Text"];

const conditionIsNew = (condition) => {
    return (
        !condition.field &&
        !condition.operator &&
        !condition.type &&
        !condition.value
    );
};

class DeviceGroupForm extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            id: undefined,
            eTag: undefined,
            displayName: "",
            mappingId: "",
            deviceID: "",
            conditions: [],
            telemetryFormat: [],
            supportedMethods: [],
            isPending: false,
            error: undefined,
            isEdit: this.props.selectedDeviceGroup,
            isDelete: undefined,
            IsPinned: false,
            SortOrder: 0,
            isConditionQuery: true,
            deviceIDList: [],
        };

        // State to input links
        this.nameLink = this.linkTo("displayName").check(
            Validator.notEmpty,
            () => this.props.t("deviceGroupsFlyout.errorMsg.nameCantBeEmpty")
        );
        this.mappingLink = this.linkTo("mappingId").map(({ value }) => value);

        this.mappingLink = this.linkTo("mappingId").map(({ value }) => value);

        this.conditionsLink = this.linkTo("conditions");
        this.subscriptions = [];
    }

    formIsValid() {
        return [this.nameLink, this.conditionsLink].every(
            (link) => !link.error
        );
    }

    componentDidMount() {
        if (this.state.isEdit) {
            this.computeState(this.props);
        }
    }

    componentWillUnmount() {
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    computeState = ({
        selectedDeviceGroup: {
            id,
            eTag,
            conditions,
            displayName,
            mappingId,
            deviceID,
            telemetryFormat,
            supportedMethods,
            isPinned,
            sortOrder,
        },
    }) => {
        if (this.state.isEdit) {
            this.setState({
                id,
                eTag,
                displayName,
                mappingId,
                deviceID,
                isConditionQuery: !(
                    conditions.length === 1 && conditions[0].key === "id"
                ),
                deviceIDList:
                    conditions.length === 1 && conditions[0].key === "id"
                        ? conditions[0].value
                        : [],
                conditions:
                    conditions.length === 1 && conditions[0].key === "id"
                        ? []
                        : conditions.map((condition) => ({
                              field: condition.key,
                              operator: condition.operator,
                              type: isNaN(condition.value) ? "Text" : "Number",
                              value: condition.value,
                              key: conditionKey++,
                          })),
                telemetryFormat: telemetryFormat,
                supportedMethods: supportedMethods,
                isPinned: isPinned,
                sortOrder: sortOrder,
            });
        }
    };

    toSelectOption = ({ id, name }) => ({ value: id, label: name });

    selectServiceCall = () => {
        if (this.state.isEdit) {
            return ConfigService.updateDeviceGroup(
                this.state.id,
                toUpdateDeviceGroupRequestModel(this.state)
            );
        }
        return ConfigService.createDeviceGroup(
            toCreateDeviceGroupRequestModel(this.state)
        );
    };

    apply = (event) => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_Save", {}));
        event.preventDefault();
        this.setState({ error: undefined, isPending: true });
        // Remove all empty rows
        this.setState(
            {
                telemetryFormat: this.state.telemetryFormat.filter(
                    (t) => t.key !== "" || t.displayName !== ""
                ),
                conditions: this.state.isConditionQuery
                    ? this.state.conditions.filter(
                          (condition) => !conditionIsNew(condition)
                      )
                    : [
                          {
                              field: "id",
                              operator: "IN",
                              type: "List",
                              value: this.state.deviceIDList,
                          },
                      ],
            },
            function () {
                this.subscriptions.push(
                    this.selectServiceCall().subscribe(
                        (deviceGroup) => {
                            this.props.insertDeviceGroups([deviceGroup]);
                            this.props.cancel();
                        },
                        (error) => this.setState({ error, isPending: false })
                    )
                );
            }.bind(this)
        );
    };

    updateTelemetryFormat = (value) =>
        this.setState({ telemetryFormat: value });
    updateSupportedMethods = (value) =>
        this.setState({ supportedMethods: value });
    addCondition = () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_AddCondition", {}));
        return this.conditionsLink.set([
            ...this.conditionsLink.value,
            newCondition(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("DeviceGroup_RemoveCondition", {})
        );
        return this.conditionsLink.set(
            this.conditionsLink.value.filter((_, idx) => index !== idx)
        );
    };

    deleteDeviceGroup = () => {
        this.setState({
            isDelete: true,
        });
    };

    onCancel = () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_Cancel", {}));
        this.props.cancel();
    };

    closeDeleteForm = () =>
        this.setState({
            isDelete: false,
        });

    tabClickHandler = (tabName) =>
        this.setState({ isConditionQuery: tabName === "Conditions" });

    addDevice = () => {
        var devices = this.state.deviceIDList;
        devices.push(this.state.deviceID);
        this.setState({ deviceIDList: devices, deviceID: "" });
    };

    removeDevice = (index) => {
        var devices = this.state.deviceIDList;
        devices.splice(index, 1);
        this.setState({ deviceIDList: devices });
    };

    render() {
        const { t } = this.props,
            // Create the state link for the dynamic form elements
            conditionLinks = this.conditionsLink.getLinkedChildren(
                (conditionLink) => {
                    const field = conditionLink
                            .forkTo("field")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.fieldCantBeEmpty"
                                )
                            ),
                        operator = conditionLink
                            .forkTo("operator")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.operatorCantBeEmpty"
                                )
                            ),
                        type = conditionLink
                            .forkTo("type")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.typeCantBeEmpty"
                                )
                            ),
                        value = conditionLink
                            .forkTo("value")
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.valueCantBeEmpty"
                                )
                            )
                            .check(
                                (val) =>
                                    type.value === "Number"
                                        ? !isNaN(val)
                                        : true,
                                t("deviceQueryConditions.errorMsg.selectedType")
                            ),
                        edited = !(
                            !field.value &&
                            !operator.value &&
                            !value.value &&
                            !type.value
                        ),
                        error =
                            (edited &&
                                (field.error ||
                                    operator.error ||
                                    value.error ||
                                    type.error)) ||
                            "";
                    return { field, operator, value, type, edited, error };
                }
            ),
            editedConditions = conditionLinks.filter(({ edited }) => edited),
            conditionHasErrors = editedConditions.some(({ error }) => !!error),
            operatorOptions = operators.map((value) => ({
                label: t(`deviceQueryConditions.operatorOptions.${value}`),
                value,
            })),
            typeOptions = valueTypes.map((value) => ({
                label: t(`deviceQueryConditions.typeOptions.${value}`),
                value,
            }));
        const { telemetryFormat, supportedMethods } = this.state;
        this.deviceIDLink = this.linkTo("deviceID")
            .check(
                Validator.listNotDuplicated,
                t(`deviceQueryConditions.errorMsg.deviceIDExists`),
                this.state.deviceIDList
            )
            .check(
                Validator.arrayExceedsLimit(50),
                t(`deviceQueryConditions.errorMsg.cannotAddMoreThan50Devices`),
                this.state.deviceIDList
            );
        return (
            <div>
                {!this.state.isDelete ? (
                    <form onSubmit={this.apply}>
                        <Section.Container
                            collapsable={false}
                            className={css("borderless")}
                        >
                            <Section.Header>
                                {this.state.isEdit
                                    ? t("deviceGroupsFlyout.edit")
                                    : t("deviceGroupsFlyout.new")}
                            </Section.Header>
                            <Section.Content>
                                <FormGroup>
                                    <FormLabel isRequired="true">
                                        {t("deviceGroupsFlyout.name")}
                                    </FormLabel>
                                    <FormControl
                                        type="text"
                                        className="long"
                                        placeholder={t(
                                            "deviceGroupsFlyout.namePlaceHolder"
                                        )}
                                        link={this.nameLink}
                                    />
                                </FormGroup>
                                <FormGroup>
                                    <FormLabel>
                                        {t("deviceGroupsFlyout.columnMapping")}
                                    </FormLabel>
                                    <FormControl
                                        type="select"
                                        ariaLabel={t(
                                            "deviceGroupsFlyout.columnMapping"
                                        )}
                                        className="long"
                                        searchable={false}
                                        clearable={false}
                                        placeholder={t(
                                            "deviceGroupsFlyout.columnMappingPlaceholder"
                                        )}
                                        options={
                                            this.props.columnMappingsOptions
                                        }
                                        link={this.mappingLink}
                                    />
                                    <Hyperlink
                                        href={`/columnMapping/custom`}
                                        className={css("new-mapping-link")}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                    >
                                        {this.props.t(
                                            "deviceGroupsFlyout.createNewMapping"
                                        )}
                                    </Hyperlink>
                                </FormGroup>
                                <div className={css("tab-container")}>
                                    <NavLink
                                        to={"#"}
                                        className={css("tab")}
                                        activeClassName={
                                            this.state.isConditionQuery
                                                ? css("active")
                                                : ""
                                        }
                                        onClick={() =>
                                            this.tabClickHandler("Conditions")
                                        }
                                    >
                                        Conditions
                                    </NavLink>
                                    <NavLink
                                        to={"#"}
                                        className={css("tab")}
                                        activeClassName={
                                            !this.state.isConditionQuery
                                                ? css("active")
                                                : ""
                                        }
                                        onClick={() =>
                                            this.tabClickHandler("Devices")
                                        }
                                    >
                                        Devices
                                    </NavLink>
                                </div>
                                {!this.state.isConditionQuery && (
                                    <FormGroup>
                                        <FormLabel
                                            className={css("device-id-label")}
                                        >
                                            {t(
                                                `deviceQueryConditions.deviceID`
                                            )}
                                        </FormLabel>
                                        <div className={css("device-id")}>
                                            <FormControl
                                                type="text"
                                                className="long"
                                                placeholder={t(
                                                    `deviceQueryConditions.enterDeviceID`
                                                )}
                                                link={this.deviceIDLink}
                                                disabled={
                                                    this.state.conditions
                                                        .length > 0
                                                }
                                            />
                                            <Btn
                                                primary
                                                disabled={
                                                    !this.state.deviceID ||
                                                    this.state.deviceID.length <
                                                        0 ||
                                                    this.state.conditions
                                                        .length > 0 ||
                                                    this.deviceIDLink.hasErrors()
                                                }
                                                onClick={this.addDevice}
                                            >
                                                Add
                                            </Btn>
                                        </div>
                                    </FormGroup>
                                )}
                                {!this.state.isConditionQuery && (
                                    <Section.Container>
                                        <Section.Header>
                                            {t(`deviceQueryConditions.devices`)}
                                        </Section.Header>
                                        <Section.Content>
                                            <div
                                                className={css("device-group")}
                                            >
                                                <div
                                                    className={css(
                                                        "device-list"
                                                    )}
                                                >
                                                    {this.state.deviceIDList.map(
                                                        (id, idx) => (
                                                            <div
                                                                className={css(
                                                                    "item"
                                                                )}
                                                                key={id}
                                                                data-index={id}
                                                            >
                                                                <img
                                                                    className={css(
                                                                        "deleteItem"
                                                                    )}
                                                                    src={
                                                                        deleteItem
                                                                    }
                                                                    onClick={() =>
                                                                        this.removeDevice(
                                                                            idx
                                                                        )
                                                                    }
                                                                    alt={
                                                                        "Remove"
                                                                    }
                                                                />
                                                                <div
                                                                    className={css(
                                                                        "title"
                                                                    )}
                                                                    key={id}
                                                                    title={id}
                                                                >
                                                                    {id}
                                                                </div>
                                                            </div>
                                                        )
                                                    )}
                                                </div>
                                            </div>
                                        </Section.Content>
                                    </Section.Container>
                                )}
                                {this.state.isConditionQuery && (
                                    <Btn
                                        className={css("add-btn")}
                                        svg={svgs.plus}
                                        onClick={this.addCondition}
                                        disabled={
                                            this.state.deviceIDList.length > 0
                                        }
                                    >
                                        {t("deviceQueryConditions.add")}
                                    </Btn>
                                )}
                                {this.state.isConditionQuery &&
                                    conditionLinks.map((condition, idx) => (
                                        <Section.Container
                                            key={this.state.conditions[idx].key}
                                        >
                                            <Section.Header>
                                                {t(
                                                    "deviceQueryConditions.condition",
                                                    {
                                                        headerCount: idx + 1,
                                                    }
                                                )}
                                            </Section.Header>
                                            <Section.Content>
                                                <FormGroup>
                                                    <FormLabel isRequired="true">
                                                        {t(
                                                            "deviceQueryConditions.field"
                                                        )}
                                                    </FormLabel>
                                                    {this.props.filtersError ? (
                                                        <AjaxError
                                                            t={t}
                                                            error={
                                                                this.props
                                                                    .filtersError
                                                            }
                                                        />
                                                    ) : (
                                                        <FormControl
                                                            type="select"
                                                            ariaLabel={t(
                                                                "deviceQueryConditions.field"
                                                            )}
                                                            className="long"
                                                            searchable={false}
                                                            clearable={false}
                                                            placeholder={t(
                                                                "deviceQueryConditions.fieldPlaceholder"
                                                            )}
                                                            options={
                                                                this.props
                                                                    .filterOptions
                                                            }
                                                            link={
                                                                condition.field
                                                            }
                                                        />
                                                    )}
                                                </FormGroup>
                                                <FormGroup>
                                                    <FormLabel isRequired="true">
                                                        {t(
                                                            "deviceQueryConditions.operator"
                                                        )}
                                                    </FormLabel>
                                                    <FormControl
                                                        type="select"
                                                        ariaLabel={t(
                                                            "deviceQueryConditions.operator"
                                                        )}
                                                        className="long"
                                                        searchable={false}
                                                        clearable={false}
                                                        options={
                                                            operatorOptions
                                                        }
                                                        placeholder={t(
                                                            "deviceQueryConditions.operatorPlaceholder"
                                                        )}
                                                        link={
                                                            condition.operator
                                                        }
                                                    />
                                                </FormGroup>
                                                <FormGroup>
                                                    <FormLabel isRequired="true">
                                                        {t(
                                                            "deviceQueryConditions.value"
                                                        )}
                                                    </FormLabel>
                                                    <FormControl
                                                        type="text"
                                                        placeholder={t(
                                                            "deviceQueryConditions.valuePlaceholder"
                                                        )}
                                                        link={condition.value}
                                                    />
                                                </FormGroup>
                                                <FormGroup>
                                                    <FormLabel isRequired="true">
                                                        {t(
                                                            "deviceQueryConditions.type"
                                                        )}
                                                    </FormLabel>
                                                    <FormControl
                                                        type="select"
                                                        ariaLabel={t(
                                                            "deviceQueryConditions.type"
                                                        )}
                                                        className="short"
                                                        clearable={false}
                                                        searchable={false}
                                                        options={typeOptions}
                                                        placeholder={t(
                                                            "deviceQueryConditions.typePlaceholder"
                                                        )}
                                                        link={condition.type}
                                                    />
                                                </FormGroup>
                                                <BtnToolbar>
                                                    <Btn
                                                        onClick={this.deleteCondition(
                                                            idx
                                                        )}
                                                    >
                                                        {t(
                                                            "deviceQueryConditions.remove"
                                                        )}
                                                    </Btn>
                                                </BtnToolbar>
                                            </Section.Content>
                                        </Section.Container>
                                    ))}
                                {this.state.isPending && (
                                    <Indicator pattern="bar" size="medium" />
                                )}

                                <DeviceGroupTelemetryFormatContainer
                                    t={t}
                                    format={telemetryFormat}
                                    onTelemetryChange={
                                        this.updateTelemetryFormat
                                    }
                                />
                                <DeviceGroupSupportedMethodsContainer
                                    t={t}
                                    methods={supportedMethods}
                                    onMethodsChange={
                                        this.updateSupportedMethods
                                    }
                                />
                                <BtnToolbar>
                                    <Protected
                                        permission={
                                            permissions.updateDeviceGroups
                                        }
                                    >
                                        <Btn
                                            primary
                                            disabled={
                                                !this.formIsValid() ||
                                                conditionHasErrors ||
                                                this.state.isPending
                                            }
                                            type="submit"
                                        >
                                            {t("deviceGroupsFlyout.save")}
                                        </Btn>
                                    </Protected>
                                    <Btn
                                        svg={svgs.cancelX}
                                        onClick={this.onCancel}
                                    >
                                        {t("deviceGroupsFlyout.cancel")}
                                    </Btn>
                                    {
                                        // Don't show delete btn if it is a new group or the group is currently active
                                        this.state.isEdit && (
                                            <Protected
                                                permission={
                                                    permissions.deleteDeviceGroups
                                                }
                                            >
                                                <Btn
                                                    svg={svgs.trash}
                                                    onClick={
                                                        this.deleteDeviceGroup
                                                    }
                                                    disabled={
                                                        this.state.isPending
                                                    }
                                                >
                                                    {t(
                                                        "deviceQueryConditions.delete"
                                                    )}
                                                </Btn>
                                            </Protected>
                                        )
                                    }
                                </BtnToolbar>
                                {this.state.error && (
                                    <AjaxError t={t} error={this.state.error} />
                                )}
                            </Section.Content>
                        </Section.Container>
                    </form>
                ) : (
                    <DeviceGroupDelete
                        {...this.props}
                        {...this.state}
                        cancelDelete={this.closeDeleteForm}
                        closeDeviceGroup={this.onCancel}
                    />
                )}
            </div>
        );
    }
}

export default DeviceGroupForm;
