// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./deviceDetails.module.scss"));

export const DeviceIcon = ({ type }) => (
    <Svg
        src={svgs.devices[(type || "generic").toLowerCase()]}
        className={css("device-icon")}
    />
);
