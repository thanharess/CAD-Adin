;;;===========================================================
;;; LỆNH: DELETELINEBYLT
;;; Xoá LINE theo linetype trong danh sách (hỗ trợ ByLayer, tự động unlock layer)
;;; Tùy chọn: xoá tất cả / chọn vùng (mặc định)
;;;===========================================================

(vl-load-com)

;; --- CẤU HÌNH LINETYPE (CHỈNH SỬA TẠI ĐÂY NẾU CẦN) ---
(setq linetypeList '("CENTER2" "CENTER" "ACAD_ISO10W100" "AM_ISO08W050" "AM_ISO09W050" 
                     "ACISOTGB" "ACANSTGB" "ACISOTGB" "AM_ISO08W050x2" "AM_ISO09W050" 
                     "PHANTOM2" "双点画线" "点画线")) ; Danh sách linetype cần xoá cho LINE

;; --- HÀM LẤY LINETYPE HIỆU QUẢ CỦA ENTITY ---
(defun get-effective-linetype (ent / entdata lt layername layerdata)
  (setq entdata (entget ent))
  (setq lt (cdr (assoc 6 entdata)))  ; Linetype của entity
  (if (not lt) (setq lt "BYLAYER"))  ; Mặc định ByLayer nếu không có code 6
  (if (= lt "BYLAYER")
    (progn
      (setq layername (cdr (assoc 8 entdata)))  ; Tên layer
      (if (and layername (setq layerdata (tblsearch "LAYER" layername)))
        (progn
          (setq lt (cdr (assoc 6 layerdata)))  ; Linetype của layer
          (if (not lt) (setq lt "CONTINUOUS"))  ; Mặc định nếu layer không có
        )
        (setq lt "CONTINUOUS")  ; Mặc định nếu không tìm thấy layer
      )
    )
  )
  lt
)

;; --- HÀM KIỂM TRA LINETYPE CÓ TRONG DANH SÁCH ---
(defun linetype-in-list (lt)
  (and lt (member lt linetypeList))
)

;; --- HÀM XỬ LÝ LOCK/UNLOCK LAYER ---
(defun handle-layer-lock (ss / i ent layername locked-layers)
  (setq locked-layers '())
  (setq i 0)
  (while (< i (sslength ss))
    (setq ent (ssname ss i))
    (setq layername (cdr (assoc 8 (entget ent))))
    (if (and layername (not (member layername locked-layers)))
      (if (= (logand (cdr (assoc 70 (tblsearch "LAYER" layername))) 4) 4)  ; Kiểm tra locked (bit 2)
        (progn
          (command "_.LAYER" "_UNLOCK" layername "")
          (setq locked-layers (cons layername locked-layers))
        )
      )
    )
    (setq i (1+ i))
  )
  (if locked-layers
    (progn
      (prompt (strcat "\n→ Đã unlock tạm thời các layer: " (vl-princ-to-string locked-layers)))
    )
  )
  locked-layers
)

;; --- HÀM RELock LAYER ---
(defun relock-layers (locked-layers / lay)
  (if locked-layers
    (progn
      (foreach lay locked-layers
        (command "_.LAYER" "_LOCK" lay "")
      )
      (prompt (strcat "\n→ Đã relock các layer: " (vl-princ-to-string locked-layers)))
    )
  )
)

(defun c:DELETELINEBYLT ( / typeStr ss-all i ent eff-lt ss-filtered count ltPrompt choice locked-layers)
  (prompt "\nLỆNH DELETELINEBYLT - Xoá LINE theo linetype trong danh sách (hỗ trợ ByLayer, tự động unlock layer).")

  ;; --- Chọn chế độ (mặc định 2: Chọn vùng) ---
  (initget 1 "1 2")
  (setq choice (getkword "\n[1:Xoá tất cả theo linetype / 2:Chọn vùng theo linetype] <2>: "))
  (if (not choice) (setq choice "2"))

  ;; Xác định loại đối tượng (chỉ LINE)
  (setq typeStr "LINE")

  ;; Tạo prompt cho linetype
  (setq ltPrompt "các linetype đã cấu hình (bao gồm ByLayer)")

  ;; --- Lấy tất cả LINE trước ---
  (cond
    ((= choice "1")
      (prompt (strcat "\n→ Tìm tất cả LINE với " ltPrompt " ..."))
      (setq ss-all (ssget "_X" (list (cons 0 typeStr))))
    )
    ((= choice "2")
      (prompt (strcat "\nChọn vùng cần xoá (sẽ lọc LINE với " ltPrompt "):"))
      (setq ss-all (ssget (list (cons 0 typeStr))))
    )
  )

  ;; --- Lọc entities theo effective linetype ---
  (if ss-all
    (progn
      (setq ss-filtered (ssadd))  ; Tạo ss mới
      (setq i 0)
      (while (< i (sslength ss-all))
        (setq ent (ssname ss-all i))
        (setq eff-lt (get-effective-linetype ent))
        (if (linetype-in-list eff-lt)
          (ssadd ent ss-filtered)
        )
        (setq i (1+ i))
      )
      ;; --- Xử lý lock layer và xoá ---
      (if (> (sslength ss-filtered) 0)
        (progn
          (setq count (sslength ss-filtered))
          (setq locked-layers (handle-layer-lock ss-filtered))
          (command "_.ERASE" ss-filtered "")
          (relock-layers locked-layers)
          (prompt (strcat "\n→ Đã xoá " (itoa count) " LINE theo linetype."))
        )
        (prompt (strcat "\nKhông tìm thấy LINE theo " ltPrompt "."))
      )
    )
    (prompt (strcat "\nKhông có LINE nào để kiểm tra với " ltPrompt "."))
  )

  (princ)
)

(princ "\nGõ DELETELINEBYLT để chạy lệnh xoá LINE theo linetype (mặc định chọn vùng).")
(princ)